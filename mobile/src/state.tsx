// App state: auth/session + server data (plans, cards) + local UI bits.
import React, {
  createContext,
  useContext,
  useEffect,
  useRef,
  useState,
} from 'react';
import { Alert } from 'react-native';
import AsyncStorage from '@react-native-async-storage/async-storage';
import * as WebBrowser from 'expo-web-browser';
import * as FileSystem from 'expo-file-system/legacy';
import * as Sharing from 'expo-sharing';
import { Theme, ThemeName, themes } from './theme';
import { nextStartISO, todayISO } from './plans';
import { Lang, TFunc, translate } from './i18n';
import { ApiCard, ApiError, ApiPayment, ApiPlan, api } from './api/client';

export type Screen = 'pass' | 'profile' | 'plans' | 'payment' | 'history';
export type PayState = 'idle' | 'processing' | 'success';

// Deep link the backend's /payments/resolve bounces the browser to; must match
// app.json "scheme" and the backend Payment:AppReturnUrl.
const PAYMENT_RETURN_URL = 'silverbreeze://payment';
export type AuthStatus = 'loading' | 'out' | 'in';

interface Session {
  token: string;
  refreshToken: string;
  userId: string;
  email: string;
}

export interface Vehicle {
  id: number;
  make: string;
  model: string;
  plate: string;
}

const THEME_KEY = 'silverbreeze.theme';
const LANG_KEY = 'silverbreeze.lang';
const SESSION_KEY = 'silverbreeze.session';
const CARDS_KEY = 'silverbreeze.cards';
const MAX_VEHICLES = 3;

interface AppState {
  theme: Theme;
  setThemeName: (name: ThemeName) => void;

  lang: Lang;
  setLang: (l: Lang) => void;
  t: TFunc;

  screen: Screen;
  setScreen: (s: Screen) => void;
  openPlans: () => void;

  // Auth
  authStatus: AuthStatus;
  email: string | null;
  token: string | null;
  authBusy: boolean;
  authError: string | null;
  register: (email: string, password: string, firstName?: string) => Promise<void>;
  login: (email: string, password: string) => Promise<void>;
  logout: () => Promise<void>;

  // Plans (from API)
  plans: ApiPlan[];
  planId: string | null;
  setPlanId: (id: string) => void;
  startDate: string;
  setStartDate: (iso: string) => void;

  // Cards (from API), sorted by start date asc
  cards: ApiCard[];
  cardsLoading: boolean;
  refreshCards: () => Promise<void>;

  // Payment history (from API)
  history: ApiPayment[];
  historyLoading: boolean;
  loadHistory: () => Promise<void>;
  openHistory: () => void;

  // Download an authenticated file (receipt PNG/PDF) to a local uri, refreshing
  // the token on 401. Returns null if unavailable.
  downloadFile: (url: string, ext: string) => Promise<string | null>;
  // Download the receipt PDF and open the system share/open sheet.
  openReceiptPdf: (url: string) => Promise<void>;

  // Payment (iPay hosted page + server-side confirmation)
  payState: PayState;
  confirmPayment: () => void;

  // Vehicles (local, cosmetic)
  vehicles: Vehicle[];
  drafts: Vehicle[];
  canAddVehicle: boolean;
  addVehicle: () => void;
  removeVehicle: (id: number) => void;
  updateDraft: (id: number, key: 'make' | 'model' | 'plate', value: string) => void;
  saveVehicle: (id: number) => void;

  notifications: boolean;
  toggleNotifications: () => void;
}

const Ctx = createContext<AppState | null>(null);

export function AppProvider({ children }: { children: React.ReactNode }) {
  const [themeName, setThemeNameState] = useState<ThemeName>('dark');
  const [lang, setLangState] = useState<Lang>('uk');
  const langRef = useRef<Lang>('uk');
  const [screen, setScreen] = useState<Screen>('pass');

  const [authStatus, setAuthStatus] = useState<AuthStatus>('loading');
  const [session, setSession] = useState<Session | null>(null);
  const sessionRef = useRef<Session | null>(null);
  const [authBusy, setAuthBusy] = useState(false);
  const [authError, setAuthError] = useState<string | null>(null);

  const [plans, setPlans] = useState<ApiPlan[]>([]);
  const [planId, setPlanId] = useState<string | null>(null);
  const [startDate, setStartDate] = useState(todayISO());

  const [cards, setCards] = useState<ApiCard[]>([]);
  const [cardsLoading, setCardsLoading] = useState(false);

  const [history, setHistory] = useState<ApiPayment[]>([]);
  const [historyLoading, setHistoryLoading] = useState(false);

  const [payState, setPayState] = useState<PayState>('idle');

  const [vehicles, setVehicles] = useState<Vehicle[]>([]);
  const [drafts, setDrafts] = useState<Vehicle[]>([]);
  const [notifications, setNotifications] = useState(true);

  const nextId = useRef(1);

  // ---- persistence ----
  useEffect(() => {
    (async () => {
      const [savedTheme, savedLang, raw] = await Promise.all([
        AsyncStorage.getItem(THEME_KEY),
        AsyncStorage.getItem(LANG_KEY),
        AsyncStorage.getItem(SESSION_KEY),
      ]);
      if (savedTheme === 'light' || savedTheme === 'dark') setThemeNameState(savedTheme);
      if (savedLang === 'uk' || savedLang === 'en') {
        setLangState(savedLang);
        langRef.current = savedLang;
      }
      if (raw) {
        try {
          const s: Session = JSON.parse(raw);
          sessionRef.current = s;
          setSession(s);
          setAuthStatus('in');
          // Show cached cards immediately (offline-friendly — the QR works without
          // network), then refresh from the API when reachable.
          const cachedCards = await AsyncStorage.getItem(CARDS_KEY);
          if (cachedCards) {
            try {
              setCards(JSON.parse(cachedCards));
            } catch {
              /* ignore corrupt cache */
            }
          }
          loadData();
          return;
        } catch {
          /* fall through */
        }
      }
      setAuthStatus('out');
    })();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // Keep the purchase start date aligned with the stacking rule as cards change
  // (after login, refresh, or a completed purchase).
  useEffect(() => {
    setStartDate(nextStartISO(cards));
  }, [cards]);

  const setThemeName = (name: ThemeName) => {
    setThemeNameState(name);
    AsyncStorage.setItem(THEME_KEY, name).catch(() => {});
  };

  const setLang = (l: Lang) => {
    setLangState(l);
    langRef.current = l;
    AsyncStorage.setItem(LANG_KEY, l).catch(() => {});
  };

  const t: TFunc = (key, vars) => translate(lang, key, vars);

  const persistSession = async (s: Session | null) => {
    sessionRef.current = s;
    setSession(s);
    if (s) await AsyncStorage.setItem(SESSION_KEY, JSON.stringify(s));
    else await AsyncStorage.removeItem(SESSION_KEY);
  };

  // Run an authed call; refresh the token once on 401, else sign out.
  const authed = async <T,>(fn: (token: string) => Promise<T>): Promise<T> => {
    const s = sessionRef.current;
    if (!s) throw new ApiError(401, 'Не авторизовано.');
    try {
      return await fn(s.token);
    } catch (e) {
      if (e instanceof ApiError && e.status === 401) {
        try {
          const r = await api.refresh(s.refreshToken);
          const ns: Session = {
            token: r.accessToken,
            refreshToken: r.refreshToken,
            userId: r.userId,
            email: s.email,
          };
          await persistSession(ns);
          return await fn(ns.token);
        } catch {
          await logout();
        }
      }
      throw e;
    }
  };

  const loadData = async () => {
    await Promise.all([loadPlans(), refreshCards()]);
  };

  const loadPlans = async () => {
    try {
      const list = await authed((tok) => api.getPlans(tok));
      setPlans(list);
      setPlanId((cur) => cur ?? list[0]?.id ?? null);
    } catch {
      /* leave plans empty; screen shows a hint */
    }
  };

  const refreshCards = async () => {
    const s = sessionRef.current;
    if (!s) return;
    setCardsLoading(true);
    try {
      const page = await authed((tok) => api.getParkingCards(s.userId, tok));
      const sorted = (page.items ?? [])
        .filter((c) => !c.isDeleted)
        .sort((a, b) => (a.startDate < b.startDate ? -1 : 1));
      setCards(sorted);
      // Cache for offline access (QR is rendered on-device from qrPayload).
      AsyncStorage.setItem(CARDS_KEY, JSON.stringify(sorted)).catch(() => {});
    } catch {
      /* keep previous cards (possibly the cached set) */
    } finally {
      setCardsLoading(false);
    }
  };

  const loadHistory = async () => {
    const s = sessionRef.current;
    if (!s) return;
    setHistoryLoading(true);
    try {
      const list = await authed((tok) => api.getPaymentsHistory(s.userId, tok));
      setHistory(list);
    } catch {
      /* keep previous */
    } finally {
      setHistoryLoading(false);
    }
  };

  const openHistory = () => {
    setScreen('history');
    loadHistory();
  };

  // Native download of an authenticated file to the cache dir (reliable for binary
  // in release builds, unlike fetch→blob). Refreshes the token once on 401.
  const downloadFile = async (url: string, ext: string): Promise<string | null> => {
    const s = sessionRef.current;
    if (!s) return null;
    const name = url.replace(/[^a-z0-9]/gi, '_').slice(-48) + '.' + ext;
    const target = (FileSystem.cacheDirectory ?? '') + name;
    const run = (tok: string) =>
      FileSystem.downloadAsync(url, target, { headers: { Authorization: `Bearer ${tok}` } });
    try {
      let res = await run(s.token);
      if (res.status === 401) {
        const r = await api.refresh(s.refreshToken);
        const ns: Session = {
          token: r.accessToken,
          refreshToken: r.refreshToken,
          userId: r.userId,
          email: s.email,
        };
        await persistSession(ns);
        res = await run(ns.token);
      }
      return res.status === 200 ? res.uri : null;
    } catch {
      return null;
    }
  };

  const openReceiptPdf = async (url: string) => {
    const uri = await downloadFile(url, 'pdf');
    if (!uri) {
      Alert.alert(translate(langRef.current, 'history.receiptTitle'),
        translate(langRef.current, 'history.receiptUnavailable'));
      return;
    }
    try {
      if (await Sharing.isAvailableAsync())
        await Sharing.shareAsync(uri, { mimeType: 'application/pdf', UTI: 'com.adobe.pdf' });
    } catch {
      /* user dismissed / no handler */
    }
  };

  // ---- auth actions ----
  const finishSignIn = async (
    r: { accessToken: string; refreshToken: string; userId: string },
    email: string
  ) => {
    await persistSession({
      token: r.accessToken,
      refreshToken: r.refreshToken,
      userId: r.userId,
      email,
    });
    setAuthStatus('in');
    setScreen('pass');
    await loadData();
  };

  const authMsg = (e: unknown, fallbackKey: string) =>
    e instanceof ApiError && e.status === 0
      ? translate(langRef.current, 'common.network')
      : e instanceof Error
        ? e.message
        : translate(langRef.current, fallbackKey);

  const login = async (email: string, password: string) => {
    setAuthBusy(true);
    setAuthError(null);
    try {
      const em = email.trim().toLowerCase();
      const r = await api.login({ email: em, password });
      await finishSignIn(r, em);
    } catch (e) {
      setAuthError(authMsg(e, 'auth.err.login'));
    } finally {
      setAuthBusy(false);
    }
  };

  const register = async (email: string, password: string, firstName?: string) => {
    setAuthBusy(true);
    setAuthError(null);
    try {
      const em = email.trim().toLowerCase();
      const reg = await api.register({ email: em, password, firstName });
      // Email is stubbed on the backend; in the test phase the confirmation
      // token is returned so we can confirm immediately.
      if (reg.devConfirmationToken) {
        await api.confirmEmail({ email: em, token: reg.devConfirmationToken });
        const r = await api.login({ email: em, password });
        await finishSignIn(r, em);
      } else {
        setAuthError(translate(langRef.current, 'auth.err.confirmManual'));
      }
    } catch (e) {
      setAuthError(authMsg(e, 'auth.err.register'));
    } finally {
      setAuthBusy(false);
    }
  };

  const logout = async () => {
    await persistSession(null);
    setPlans([]);
    setCards([]);
    setHistory([]);
    AsyncStorage.removeItem(CARDS_KEY).catch(() => {});
    setPlanId(null);
    setVehicles([]);
    setDrafts([]);
    setPayState('idle');
    setStartDate(todayISO());
    setScreen('pass');
    setAuthStatus('out');
  };

  // ---- navigation / buy ----
  const openPlans = () => {
    setStartDate(nextStartISO(cards));
    setPlanId((cur) => cur ?? plans[0]?.id ?? null);
    setScreen('plans');
  };

  // Poll the payment until it leaves Pending (the resolve endpoint sets the final
  // state server-side). Returns the terminal status, or 'Pending' if it never settled.
  const pollPaymentStatus = async (paymentId: string): Promise<string> => {
    for (let i = 0; i < 10; i++) {
      try {
        const p = await authed((tok) => api.getPayment(paymentId, tok));
        if (p.status !== 'Pending') return p.status;
      } catch {
        /* transient — retry */
      }
      await new Promise((r) => setTimeout(r, 1500));
    }
    return 'Pending';
  };

  const confirmPayment = () => {
    const s = sessionRef.current;
    if (payState !== 'idle' || !s || !planId) return;
    setPayState('processing');
    (async () => {
      try {
        const init = await authed((tok) =>
          api.initiatePayment(
            { userId: s.userId, subscriptionPlanId: planId, startDate },
            tok
          )
        );

        // Open the iPay hosted page; resolves when the backend bounces the
        // browser back to our deep link (silverbreeze://payment?...).
        const result = await WebBrowser.openAuthSessionAsync(
          init.redirectUrl,
          PAYMENT_RETURN_URL
        );

        if (result.type !== 'success') {
          // User dismissed the browser — check the status anyway in case they paid.
          const status = await pollPaymentStatus(init.paymentId);
          if (status !== 'Succeeded') {
            setPayState('idle');
            return;
          }
        } else {
          const status = await pollPaymentStatus(init.paymentId);
          if (status !== 'Succeeded') {
            setPayState('idle');
            Alert.alert(
              translate(langRef.current, 'pay.err.title'),
              translate(langRef.current, 'pay.err.notCompleted')
            );
            return;
          }
        }

        setPayState('success');
        await refreshCards();
        setTimeout(() => {
          setPayState('idle');
          setScreen('pass');
        }, 1200);
      } catch (e) {
        setPayState('idle');
        Alert.alert(
          translate(langRef.current, 'pay.err.title'),
          e instanceof Error ? e.message : translate(langRef.current, 'pay.err.body')
        );
      }
    })();
  };

  // ---- vehicles (local) ----
  const addVehicle = () => {
    if (vehicles.length >= MAX_VEHICLES) return;
    const blank: Vehicle = { id: nextId.current++, make: '', model: '', plate: '' };
    setVehicles((v) => [...v, blank]);
    setDrafts((d) => [...d, { ...blank }]);
  };
  const removeVehicle = (id: number) => {
    setVehicles((v) => v.filter((x) => x.id !== id));
    setDrafts((d) => d.filter((x) => x.id !== id));
  };
  const updateDraft = (
    id: number,
    key: 'make' | 'model' | 'plate',
    value: string
  ) => {
    setDrafts((d) => d.map((x) => (x.id === id ? { ...x, [key]: value } : x)));
  };
  const saveVehicle = (id: number) => {
    const draft = drafts.find((x) => x.id === id);
    if (!draft) return;
    setVehicles((v) => v.map((x) => (x.id === id ? { ...draft } : x)));
  };

  const value: AppState = {
    theme: themes[themeName],
    setThemeName,
    lang,
    setLang,
    t,
    screen,
    setScreen,
    openPlans,

    authStatus,
    email: session?.email ?? null,
    token: session?.token ?? null,
    authBusy,
    authError,
    register,
    login,
    logout,

    plans,
    planId,
    setPlanId,
    startDate,
    setStartDate,

    cards,
    cardsLoading,
    refreshCards,

    history,
    historyLoading,
    loadHistory,
    openHistory,
    downloadFile,
    openReceiptPdf,

    payState,
    confirmPayment,

    vehicles,
    drafts,
    canAddVehicle: vehicles.length < MAX_VEHICLES,
    addVehicle,
    removeVehicle,
    updateDraft,
    saveVehicle,

    notifications,
    toggleNotifications: () => setNotifications((n) => !n),
  };

  return <Ctx.Provider value={value}>{children}</Ctx.Provider>;
}

export function useApp(): AppState {
  const ctx = useContext(Ctx);
  if (!ctx) throw new Error('useApp must be used inside AppProvider');
  return ctx;
}
