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
import { Theme, ThemeName, themes } from './theme';
import { todayISO } from './plans';
import { ApiCard, ApiError, ApiPlan, api } from './api/client';

export type Screen = 'pass' | 'profile' | 'plans' | 'payment';
export type PayMethod = 'applepay' | 'card';
export type PayState = 'idle' | 'processing' | 'success';
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
const SESSION_KEY = 'silverbreeze.session';
const MAX_VEHICLES = 3;

interface AppState {
  theme: Theme;
  setThemeName: (name: ThemeName) => void;

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

  // Payment
  payMethod: PayMethod;
  setPayMethod: (m: PayMethod) => void;
  cardNumber: string;
  setCardNumber: (v: string) => void;
  cardExpiry: string;
  setCardExpiry: (v: string) => void;
  cardCvc: string;
  setCardCvc: (v: string) => void;
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

  const [payMethod, setPayMethod] = useState<PayMethod>('card');
  const [cardNumber, setCardNumber] = useState('');
  const [cardExpiry, setCardExpiry] = useState('');
  const [cardCvc, setCardCvc] = useState('');
  const [payState, setPayState] = useState<PayState>('idle');

  const [vehicles, setVehicles] = useState<Vehicle[]>([]);
  const [drafts, setDrafts] = useState<Vehicle[]>([]);
  const [notifications, setNotifications] = useState(true);

  const nextId = useRef(1);

  // ---- persistence ----
  useEffect(() => {
    (async () => {
      const [t, raw] = await Promise.all([
        AsyncStorage.getItem(THEME_KEY),
        AsyncStorage.getItem(SESSION_KEY),
      ]);
      if (t === 'light' || t === 'dark') setThemeNameState(t);
      if (raw) {
        try {
          const s: Session = JSON.parse(raw);
          sessionRef.current = s;
          setSession(s);
          setAuthStatus('in');
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

  const setThemeName = (name: ThemeName) => {
    setThemeNameState(name);
    AsyncStorage.setItem(THEME_KEY, name).catch(() => {});
  };

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
    } catch {
      /* keep previous cards */
    } finally {
      setCardsLoading(false);
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

  const login = async (email: string, password: string) => {
    setAuthBusy(true);
    setAuthError(null);
    try {
      const em = email.trim().toLowerCase();
      const r = await api.login({ email: em, password });
      await finishSignIn(r, em);
    } catch (e) {
      setAuthError(e instanceof Error ? e.message : 'Не вдалося увійти.');
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
        setAuthError('Перевірте пошту й підтвердіть акаунт, потім увійдіть.');
      }
    } catch (e) {
      setAuthError(e instanceof Error ? e.message : 'Не вдалося зареєструватися.');
    } finally {
      setAuthBusy(false);
    }
  };

  const logout = async () => {
    await persistSession(null);
    setPlans([]);
    setCards([]);
    setPlanId(null);
    setVehicles([]);
    setDrafts([]);
    setCardNumber('');
    setCardExpiry('');
    setCardCvc('');
    setPayState('idle');
    setStartDate(todayISO());
    setScreen('pass');
    setAuthStatus('out');
  };

  // ---- navigation / buy ----
  const openPlans = () => {
    setStartDate(todayISO());
    setPlanId((cur) => cur ?? plans[0]?.id ?? null);
    setScreen('plans');
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
        await api.completePaymentDev(init.providerPaymentId);
        setPayState('success');
        await refreshCards();
        setCardNumber('');
        setCardExpiry('');
        setCardCvc('');
        setTimeout(() => {
          setPayState('idle');
          setScreen('pass');
        }, 1200);
      } catch (e) {
        setPayState('idle');
        Alert.alert(
          'Оплата не пройшла',
          e instanceof Error ? e.message : 'Спробуйте ще раз.'
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

    payMethod,
    setPayMethod,
    cardNumber,
    setCardNumber,
    cardExpiry,
    setCardExpiry,
    cardCvc,
    setCardCvc,
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
