// Single app-state context, mirroring the prototype's component state.
// Navigation is a plain `screen` value (two tabs + Plans/Payment reached
// only via "Manage plan"), so no navigation library is needed yet.
import React, {
  createContext,
  useContext,
  useEffect,
  useRef,
  useState,
} from 'react';
import AsyncStorage from '@react-native-async-storage/async-storage';
import { Theme, ThemeName, themes } from './theme';
import { PlanId, endDate, planMonths, toLocalISO, todayISO } from './plans';

export type Screen = 'pass' | 'profile' | 'plans' | 'payment';
export type PayMethod = 'applepay' | 'card';
export type PayState = 'idle' | 'processing' | 'success';

export interface Subscription {
  id: number;
  planId: PlanId;
  startDate: string; // local ISO
}

export interface Vehicle {
  id: number;
  make: string;
  model: string;
  plate: string;
}

const THEME_KEY = 'silverbreeze.theme';
const MAX_VEHICLES = 3;

interface AppState {
  theme: Theme;
  setThemeName: (name: ThemeName) => void;

  screen: Screen;
  setScreen: (s: Screen) => void;
  /** "Manage plan": open Plans pre-filled so the new period never overlaps. */
  openPlans: () => void;
  /** Clear the account back to an empty state (no backend session yet). */
  signOut: () => void;

  planId: PlanId;
  setPlanId: (p: PlanId) => void;
  startDate: string;
  setStartDate: (iso: string) => void;

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

  /** Sorted ascending by start date; [0] is the active one. */
  subscriptions: Subscription[];

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
  const [planId, setPlanId] = useState<PlanId>('m1');
  const [startDate, setStartDate] = useState(todayISO());

  const [payMethod, setPayMethod] = useState<PayMethod>('card');
  const [cardNumber, setCardNumber] = useState('');
  const [cardExpiry, setCardExpiry] = useState('');
  const [cardCvc, setCardCvc] = useState('');
  const [payState, setPayState] = useState<PayState>('idle');

  // Empty account by default — no seeded subscription or vehicle.
  const [subscriptions, setSubscriptions] = useState<Subscription[]>([]);

  const [vehicles, setVehicles] = useState<Vehicle[]>([]);
  const [drafts, setDrafts] = useState<Vehicle[]>([]);
  const [notifications, setNotifications] = useState(true);

  const nextId = useRef(1);

  useEffect(() => {
    AsyncStorage.getItem(THEME_KEY).then((v) => {
      if (v === 'light' || v === 'dark') setThemeNameState(v);
    });
  }, []);

  const setThemeName = (name: ThemeName) => {
    setThemeNameState(name);
    AsyncStorage.setItem(THEME_KEY, name).catch(() => {});
  };

  const openPlans = () => {
    // Empty account: start a first pass today with a sensible default.
    if (subscriptions.length === 0) {
      setStartDate(todayISO());
      setPlanId('m1');
      setScreen('plans');
      return;
    }
    // Next start = day after the latest end date across all subscriptions.
    const lastEnd = subscriptions
      .map((s) => endDate(s.startDate, planMonths(s.planId)))
      .sort()
      .slice(-1)[0];
    const next = new Date(lastEnd + 'T00:00:00');
    next.setDate(next.getDate() + 1);
    const latest = [...subscriptions].sort((a, b) =>
      a.startDate < b.startDate ? 1 : -1
    )[0];
    setStartDate(toLocalISO(next));
    setPlanId(latest.planId);
    setScreen('plans');
  };

  const signOut = () => {
    setSubscriptions([]);
    setVehicles([]);
    setDrafts([]);
    setCardNumber('');
    setCardExpiry('');
    setCardCvc('');
    setPayState('idle');
    setPlanId('m1');
    setStartDate(todayISO());
    setNotifications(true);
    nextId.current = 1;
    setScreen('pass');
  };

  const confirmPayment = () => {
    if (payState !== 'idle') return;
    setPayState('processing');
    setTimeout(() => setPayState('success'), 1100);
    setTimeout(() => {
      setSubscriptions((prev) =>
        [
          ...prev,
          { id: nextId.current++, planId, startDate },
        ].sort((a, b) => (a.startDate < b.startDate ? -1 : 1))
      );
      // Card details are never stored — clear them after every checkout.
      setCardNumber('');
      setCardExpiry('');
      setCardCvc('');
      setPayState('idle');
      setScreen('pass');
    }, 2200);
  };

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
    signOut,
    planId,
    setPlanId,
    startDate,
    setStartDate,
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
    subscriptions,
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
