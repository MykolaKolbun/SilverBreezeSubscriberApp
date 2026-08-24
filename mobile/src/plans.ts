// Display helpers. Plan data now comes from the backend (GET /plans).

export type PlanKind = 'covered' | 'outdoor';

export const KIND_LABEL: Record<PlanKind, string> = {
  covered: 'Паркінг',
  outdoor: 'Зовнішній паркінг',
};

// Plan codes are PARK_* (covered) and OUT_* (outdoor).
export const planKind = (code: string): PlanKind =>
  code.startsWith('OUT') ? 'outdoor' : 'covered';

export const uahFromMinor = (minor: number) => minor / 100;

// "3600" -> "3 600 ₴" (manual grouping — no Intl dependency on Hermes).
export const fmtUAH = (uah: number) =>
  Math.round(uah)
    .toString()
    .replace(/\B(?=(\d{3})+(?!\d))/g, ' ') + ' ₴';

// ---- dates (local YYYY-MM-DD strings) ----

export function toLocalISO(d: Date): string {
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${y}-${m}-${day}`;
}

export const todayISO = () => toLocalISO(new Date());

export function fmtDate(iso: string): string {
  if (!iso) return '';
  const d = new Date(iso.slice(0, 10) + 'T00:00:00');
  return d.toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
}
