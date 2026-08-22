// Plan catalog + date/price rules from the handoff prototype.
// Prices are local-only for now; swap for GET /plans from the backend later.

export type PlanId = 'basic' | 'plus' | 'unlimited';
export type Billing = 'monthly' | 'annual';

export interface Plan {
  id: PlanId;
  label: string;
  base: number; // monthly price in EUR
  features: [string, string];
  popular?: boolean;
}

export const PLANS: Plan[] = [
  {
    id: 'basic',
    label: 'Basic',
    base: 39,
    features: ['Nights & weekends only', '1 vehicle · standard spaces'],
  },
  {
    id: 'plus',
    label: 'Plus',
    base: 69,
    features: ['Anytime access', '1 vehicle · reserved level'],
    popular: true,
  },
  {
    id: 'unlimited',
    label: 'Unlimited',
    base: 99,
    features: ['Anytime access', '2 vehicles · reserved level + EV bay'],
  },
];

export const planLabel = (id: PlanId) =>
  PLANS.find((p) => p.id === id)!.label;

// Annual = monthly × 10 (2 months free).
export function price(id: PlanId, billing: Billing): number {
  const base = PLANS.find((p) => p.id === id)!.base;
  return billing === 'annual' ? Math.round(base * 10) : base;
}

export const periodSuffix = (billing: Billing) =>
  billing === 'annual' ? '/ year' : '/ month';

export const fmtEuro = (n: number) => '€' + n.toFixed(2);

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

// End date = start + 1 billing period − 1 day (Jul 12 monthly → Aug 11).
export function endDate(startISO: string, billing: Billing): string {
  const d = new Date(startISO + 'T00:00:00');
  if (billing === 'annual') d.setFullYear(d.getFullYear() + 1);
  else d.setMonth(d.getMonth() + 1);
  d.setDate(d.getDate() - 1);
  return toLocalISO(d);
}
