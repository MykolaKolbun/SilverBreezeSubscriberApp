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
  id: number; // stable local key (for React + drafts)
  serverId?: string; // backend Guid once synced; absent for offline-created rows
  dirty?: boolean; // has unsynced local changes to push
  make: string;
  model: string;
  plate: string;
}

export interface Profile {
  firstName: string;
  surname: string;
  mobile: string;
}

const THEME_KEY = 'silverbreeze.theme';
const LANG_KEY = 'silverbreeze.lang';
const SESSION_KEY = 'silverbreeze.session';
const CARDS_KEY = 'silverbreeze.cards';
const VEHICLES_KEY = 'silverbreeze.vehicles';
const PROFILE_KEY = 'silverbreeze.profile';
const MAX_VEHICLES = 3;
const EMPTY_PROFILE: Profile = { firstName: '', surname: '', mobile: '' };

interface AppState {
  theme: Theme;
  setThemeName: (name: ThemeName) => void;

  lang: Lang;
  setLang: (l: Lang) => void;
  t: TFunc;

  screen: Screen;
  setScreen: (s: Screen) => void;
  openPlans: () => void;

  // Auth (passwordless email + OTP)
  authStatus: AuthStatus;
  email: string | null;
  token: string | null;
  authBusy: boolean;
  authError: string | null;
  // Request an email code; ok=false on failure. devCode is set while email is stubbed (dev autofill).
  requestEmailCode: (email: string) => Promise<{ ok: boolean; devCode: string | null }>;
  verifyEmailCode: (email: string, code: string) => Promise<void>;
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

  // Profile (name + phone), bidirectional offline sync
  profileDraft: Profile;
  profileChanged: boolean;
  updateProfileField: (key: keyof Profile, value: string) => void;
  saveProfile: () => void;

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

  const [profile, setProfile] = useState<Profile>(EMPTY_PROFILE);
  const [profileDraft, setProfileDraft] = useState<Profile>(EMPTY_PROFILE);
  const profileRef = useRef<Profile>(EMPTY_PROFILE);
  const profileDirtyRef = useRef(false);
  const profileSyncingRef = useRef(false);

  const [vehicles, setVehicles] = useState<Vehicle[]>([]);
  const [drafts, setDrafts] = useState<Vehicle[]>([]);
  const vehiclesRef = useRef<Vehicle[]>([]);
  const deletedRef = useRef<string[]>([]); // serverIds removed offline, pending delete
  const syncingRef = useRef(false);
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
          // Load cached profile (offline-friendly), then sync bidirectionally.
          const rawProfile = await AsyncStorage.getItem(PROFILE_KEY);
          if (rawProfile) {
            try {
              const parsed = JSON.parse(rawProfile);
              const p: Profile = { ...EMPTY_PROFILE, ...(parsed.profile ?? {}) };
              profileRef.current = p;
              profileDirtyRef.current = parsed.dirty ?? false;
              setProfile(p);
              setProfileDraft({ ...p });
            } catch {
              /* ignore corrupt cache */
            }
          }
          // Load cached vehicles (offline-friendly), then sync bidirectionally.
          const rawVeh = await AsyncStorage.getItem(VEHICLES_KEY);
          if (rawVeh) {
            try {
              const parsed = JSON.parse(rawVeh);
              const vs: Vehicle[] = parsed.vehicles ?? [];
              deletedRef.current = parsed.deleted ?? [];
              vehiclesRef.current = vs;
              setVehicles(vs);
              setDrafts(vs.map((v) => ({ ...v })));
              nextId.current = vs.reduce((m, v) => Math.max(m, v.id), 0) + 1;
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
    // Bidirectional sync: push local changes first, then pull server state.
    await Promise.all([syncProfile(true), syncVehicles(true)]);
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

  const requestEmailCode = async (
    email: string
  ): Promise<{ ok: boolean; devCode: string | null }> => {
    setAuthBusy(true);
    setAuthError(null);
    try {
      const r = await api.requestEmailCode(email.trim().toLowerCase());
      return { ok: true, devCode: r.devCode ?? null };
    } catch (e) {
      setAuthError(authMsg(e, 'auth.err.email'));
      return { ok: false, devCode: null };
    } finally {
      setAuthBusy(false);
    }
  };

  const verifyEmailCode = async (email: string, code: string) => {
    setAuthBusy(true);
    setAuthError(null);
    try {
      const em = email.trim().toLowerCase();
      const r = await api.verifyEmailCode(em, code);
      await finishSignIn(r, em);
    } catch (e) {
      setAuthError(authMsg(e, 'auth.err.code'));
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
    AsyncStorage.removeItem(VEHICLES_KEY).catch(() => {});
    AsyncStorage.removeItem(PROFILE_KEY).catch(() => {});
    setPlanId(null);
    profileRef.current = EMPTY_PROFILE;
    profileDirtyRef.current = false;
    setProfile(EMPTY_PROFILE);
    setProfileDraft(EMPTY_PROFILE);
    vehiclesRef.current = [];
    deletedRef.current = [];
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

  // ---- profile: name + phone (offline-first, bidirectional sync) ----
  const persistProfile = () =>
    AsyncStorage.setItem(
      PROFILE_KEY,
      JSON.stringify({ profile: profileRef.current, dirty: profileDirtyRef.current })
    ).catch(() => {});

  const updateProfileField = (key: keyof Profile, value: string) =>
    setProfileDraft((d) => ({ ...d, [key]: value }));

  const saveProfile = () => {
    const next: Profile = { ...profileDraft };
    profileRef.current = next;
    setProfile(next);
    profileDirtyRef.current = true;
    persistProfile();
    void syncProfile(false);
  };

  const pushProfile = async () => {
    const s = sessionRef.current;
    if (!s || !profileDirtyRef.current) return;
    try {
      const p = profileRef.current;
      await authed((tok) =>
        api.updateUser(s.userId, { firstName: p.firstName, surname: p.surname, mobile: p.mobile }, tok)
      );
      profileDirtyRef.current = false;
      persistProfile();
    } catch {
      /* offline/other: keep dirty, retry next sync */
    }
  };

  const pullProfile = async () => {
    const s = sessionRef.current;
    if (!s) return;
    try {
      const u = await authed((tok) => api.getUser(s.userId, tok));
      if (profileDirtyRef.current) return; // keep not-yet-pushed local edits
      const next: Profile = {
        firstName: u.firstName ?? '',
        surname: u.surname ?? '',
        mobile: u.mobile ?? '',
      };
      profileRef.current = next;
      setProfile(next);
      setProfileDraft({ ...next });
      persistProfile();
    } catch {
      /* offline — keep the cached profile */
    }
  };

  const syncProfile = async (pull: boolean) => {
    if (!sessionRef.current || profileSyncingRef.current) return;
    profileSyncingRef.current = true;
    try {
      await pushProfile();
      if (pull) await pullProfile();
    } finally {
      profileSyncingRef.current = false;
    }
  };

  // ---- vehicles (offline-first, bidirectional sync) ----
  const persistVehicles = () =>
    AsyncStorage.setItem(
      VEHICLES_KEY,
      JSON.stringify({ vehicles: vehiclesRef.current, deleted: deletedRef.current })
    ).catch(() => {});

  // Update the saved-vehicle list + ref + local cache in one place.
  const commitVehicles = (next: Vehicle[]) => {
    vehiclesRef.current = next;
    setVehicles(next);
    persistVehicles();
  };

  const addVehicle = () => {
    if (vehiclesRef.current.length >= MAX_VEHICLES) return;
    const blank: Vehicle = { id: nextId.current++, make: '', model: '', plate: '' };
    commitVehicles([...vehiclesRef.current, blank]);
    setDrafts((d) => [...d, { ...blank }]);
  };

  const removeVehicle = (id: number) => {
    const target = vehiclesRef.current.find((x) => x.id === id);
    if (target?.serverId) {
      // Queue the remote delete; it's replayed on the next sync (works offline).
      deletedRef.current = [...deletedRef.current, target.serverId];
    }
    commitVehicles(vehiclesRef.current.filter((x) => x.id !== id));
    setDrafts((d) => d.filter((x) => x.id !== id));
    void syncVehicles(false);
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
    // Commit the draft and mark it dirty so the sync pushes it to the backend.
    commitVehicles(
      vehiclesRef.current.map((x) =>
        x.id === id ? { ...x, make: draft.make, model: draft.model, plate: draft.plate, dirty: true } : x
      )
    );
    void syncVehicles(false);
  };

  // Push queued deletes + dirty creates/updates to the backend. Best-effort:
  // stops on a network error (offline) and retries on the next sync.
  const pushVehicles = async () => {
    const s = sessionRef.current;
    if (!s) return;

    // Deletes first.
    for (const serverId of [...deletedRef.current]) {
      try {
        await authed((tok) => api.deleteVehicle(serverId, tok));
        deletedRef.current = deletedRef.current.filter((x) => x !== serverId);
      } catch (e) {
        if (e instanceof ApiError && e.status === 0) return; // offline — retry later
        // 404/gone → treat as already deleted.
        deletedRef.current = deletedRef.current.filter((x) => x !== serverId);
      }
    }
    persistVehicles();

    // Creates / updates for dirty rows that have a plate.
    for (const v of [...vehiclesRef.current]) {
      if (!v.dirty || !v.plate.trim()) continue;
      try {
        if (v.serverId) {
          await authed((tok) =>
            api.updateVehicle(v.serverId!, { plateNumber: v.plate, make: v.make, model: v.model }, tok)
          );
          markVehicleSynced(v.id, v.serverId);
        } else {
          const dto = await authed((tok) =>
            api.createVehicle({ userId: s.userId, plateNumber: v.plate, make: v.make, model: v.model }, tok)
          );
          markVehicleSynced(v.id, dto.id);
        }
      } catch (e) {
        if (e instanceof ApiError && e.status === 0) return; // offline — retry later
        /* other errors: leave dirty, retry next sync */
      }
    }
  };

  const markVehicleSynced = (localId: number, serverId: string) =>
    commitVehicles(
      vehiclesRef.current.map((x) =>
        x.id === localId ? { ...x, serverId, dirty: false } : x
      )
    );

  // Pull the server list and make it authoritative, keeping any not-yet-pushed
  // offline creations so nothing is lost.
  const pullVehicles = async () => {
    const s = sessionRef.current;
    if (!s) return;
    try {
      const list = await authed((tok) => api.getVehicles(s.userId, tok));
      const pendingCreates = vehiclesRef.current.filter((v) => v.dirty && !v.serverId);
      const fromServer: Vehicle[] = list.map((d) => ({
        id: nextId.current++,
        serverId: d.id,
        make: d.make ?? '',
        model: d.model ?? '',
        plate: d.plateNumber,
      }));
      const merged = [...fromServer, ...pendingCreates].slice(0, MAX_VEHICLES);
      commitVehicles(merged);
      setDrafts(merged.map((v) => ({ ...v })));
    } catch {
      /* offline — keep the cached list */
    }
  };

  const syncVehicles = async (pull: boolean) => {
    if (!sessionRef.current || syncingRef.current) return;
    syncingRef.current = true;
    try {
      await pushVehicles();
      if (pull) await pullVehicles();
    } finally {
      syncingRef.current = false;
    }
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
    requestEmailCode,
    verifyEmailCode,
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

    profileDraft,
    profileChanged:
      profileDraft.firstName !== profile.firstName ||
      profileDraft.surname !== profile.surname ||
      profileDraft.mobile !== profile.mobile,
    updateProfileField,
    saveProfile,

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
