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
import { Billing, PlanId, endDate, toLocalISO, todayISO } from './plans';

export type Screen = 'pass' | 'profile' | 'plans' | 'payment';
export type PayMethod = 'applepay' | 'card';
export type PayState = 'idle' | 'processing' | 'success';

export interface Subscription {
  id: number;
  planId: PlanId;
  billing: Billing;
  startDate: string; // local ISO
}

export interface Vehicle {
  id: number;
  make: string;
  model: string;
  plate: string;
}

const THEME_KEY = 'parking-pass.theme';
const MAX_VEHICLES = 3;

interface AppState {
  theme: Theme;
  setThemeName: (name: ThemeName) => void;

  screen: Screen;
  setScreen: (s: Screen) => void;
  /** "Manage plan": open Plans pre-filled so the new period never overlaps. */
  openPlans: () => void;

  billing: Billing;
  setBilling: (b: Billing) => void;
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

const initialVehicle: Vehicle = {
  id: 1,
  make: 'Tesla',
  model: 'Model 3',
  plate: '34 ABC 567',
};

export function AppProvider({ children }: { children: React.ReactNode }) {
  const [themeName, setThemeNameState] = useState<ThemeName>('dark');
  const [screen, setScreen] = useState<Screen>('pass');
  const [billing, setBilling] = useState<Billing>('monthly');
  const [planId, setPlanId] = useState<PlanId>('plus');
  const [startDate, setStartDate] = useState(todayISO());

  const [payMethod, setPayMethod] = useState<PayMethod>('card');
  const [cardNumber, setCardNumber] = useState('');
  const [cardExpiry, setCardExpiry] = useState('');
  const [cardCvc, setCardCvc] = useState('');
  const [payState, setPayState] = useState<PayState>('idle');

  const [subscriptions, setSubscriptions] = useState<Subscription[]>([
    { id: 1, planId: 'plus', billing: 'monthly', startDate: todayISO() },
  ]);

  const [vehicles, setVehicles] = useState<Vehicle[]>([initialVehicle]);
  const [drafts, setDrafts] = useState<Vehicle[]>([{ ...initialVehicle }]);
  const [notifications, setNotifications] = useState(true);

  const nextId = useRef(2);

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
    // Next start = day after the latest end date across all subscriptions.
    const lastEnd = subscriptions
      .map((s) => endDate(s.startDate, s.billing))
      .sort()
      .slice(-1)[0];
    const next = new Date(lastEnd + 'T00:00:00');
    next.setDate(next.getDate() + 1);
    const latest = [...subscriptions].sort((a, b) =>
      a.startDate < b.startDate ? 1 : -1
    )[0];
    setStartDate(toLocalISO(next));
    setPlanId(latest.planId);
    setBilling(latest.billing);
    setScreen('plans');
  };

  const confirmPayment = () => {
    if (payState !== 'idle') return;
    setPayState('processing');
    setTimeout(() => setPayState('success'), 1100);
    setTimeout(() => {
      setSubscriptions((prev) =>
        [
          ...prev,
          { id: nextId.current++, planId, billing, startDate },
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
    billing,
    setBilling,
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
