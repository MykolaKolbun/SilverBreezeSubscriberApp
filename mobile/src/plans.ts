// Plan catalog + date/price rules.
// Three fixed-duration parking passes, priced in UAH.
// Swap for GET /plans from the backend later.

export type PlanId = 'm1' | 'm2' | 'm3' | 'out1' | 'out3';
export type PlanKind = 'covered' | 'outdoor';

export interface Plan {
  id: PlanId;
  kind: PlanKind;
  label: string; // "1 місяць"
  months: number; // duration in months
  price: number; // total price in UAH
}

export const PLANS: Plan[] = [
  { id: 'm1', kind: 'covered', label: '1 місяць', months: 1, price: 3600 },
  { id: 'm2', kind: 'covered', label: '2 місяці', months: 2, price: 7200 },
  { id: 'm3', kind: 'covered', label: '3 місяці', months: 3, price: 10800 },
  { id: 'out1', kind: 'outdoor', label: '1 місяць', months: 1, price: 3000 },
  { id: 'out3', kind: 'outdoor', label: '3 місяці', months: 3, price: 9000 },
];

export const KIND_LABEL: Record<PlanKind, string> = {
  covered: 'Паркінг',
  outdoor: 'Зовнішній паркінг',
};

const planById = (id: PlanId) => PLANS.find((p) => p.id === id)!;

// Disambiguated label — outdoor plans are prefixed so they read clearly
// wherever a single plan is shown (pass, payment, profile).
export const planLabel = (id: PlanId) => {
  const p = planById(id);
  return p.kind === 'outdoor' ? `Зовнішній · ${p.label}` : p.label;
};
export const planMonths = (id: PlanId) => planById(id).months;
export const price = (id: PlanId) => planById(id).price;

// "3600" -> "3 600 ₴" (manual grouping — no Intl dependency on Hermes).
export const fmtUAH = (n: number) =>
  n.toString().replace(/\B(?=(\d{3})+(?!\d))/g, ' ') + ' ₴';

// ---- dates (all as local YYYY-MM-DD strings) ----

export function toLocalISO(d: Date): string {
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${y}-${m}-${day}`;
}

export const todayISO = () => toLocalISO(new Date());

export function fmtDate(iso: string): string {
  if (!iso) return '';
  const d = new Date(iso + 'T00:00:00');
  return d.toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
}

// End date = start + N months − 1 day (Jul 12 + 1 month → Aug 11).
export function endDate(startISO: string, months: number): string {
  const d = new Date(startISO + 'T00:00:00');
  d.setMonth(d.getMonth() + months);
  d.setDate(d.getDate() - 1);
  return toLocalISO(d);
}
