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

export const monthsFromDuration = (days: number) => Math.max(1, Math.round(days / 30));

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

const MONTHS: Record<'uk' | 'en', string[]> = {
  uk: ['січ', 'лют', 'бер', 'кві', 'тра', 'чер', 'лип', 'сер', 'вер', 'жов', 'лис', 'гру'],
  en: ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'],
};

// Full month names (uk in genitive: "23 вересня"; en: "September 23").
const MONTHS_FULL: Record<'uk' | 'en', string[]> = {
  uk: ['січня', 'лютого', 'березня', 'квітня', 'травня', 'червня',
       'липня', 'серпня', 'вересня', 'жовтня', 'листопада', 'грудня'],
  en: ['January', 'February', 'March', 'April', 'May', 'June',
       'July', 'August', 'September', 'October', 'November', 'December'],
};

// Manual month names (Hermes has no reliable Intl month localization).
export function fmtDate(iso: string, lang: 'uk' | 'en' = 'uk'): string {
  if (!iso) return '';
  const d = new Date(iso.slice(0, 10) + 'T00:00:00');
  const m = MONTHS[lang][d.getMonth()];
  return lang === 'uk' ? `${d.getDate()} ${m}` : `${m} ${d.getDate()}`;
}

export function fmtDateFull(iso: string, lang: 'uk' | 'en' = 'uk'): string {
  if (!iso) return '';
  const d = new Date(iso.slice(0, 10) + 'T00:00:00');
  const m = MONTHS_FULL[lang][d.getMonth()];
  return lang === 'uk' ? `${d.getDate()} ${m}` : `${m} ${d.getDate()}`;
}

// Next start date under the stacking rule: the day after the user's latest active
// (non-deleted, not-yet-ended) card ends; today if there are none. Mirrors the
// backend's OnSucceededAsync so the app shows the date the card will actually get.
export function nextStartISO(
  cards: { status: string; endDate: string; isDeleted: boolean }[]
): string {
  const today = todayISO();
  let latestEnd = '';
  for (const c of cards) {
    if (c.isDeleted || c.status !== 'Active') continue;
    if (c.endDate > latestEnd) latestEnd = c.endDate;
  }
  if (latestEnd && latestEnd >= today) {
    const d = new Date(latestEnd.slice(0, 10) + 'T00:00:00');
    d.setDate(d.getDate() + 1);
    return toLocalISO(d);
  }
  return today;
}
