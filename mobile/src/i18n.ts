// Lightweight i18n: uk/en dictionaries + a translate() with {var} interpolation.
export type Lang = 'uk' | 'en';
export type TFunc = (key: string, vars?: Record<string, string | number>) => string;

type Dict = Record<string, string>;

const uk: Dict = {
  'nav.pass': 'Абонемент',
  'nav.profile': 'Профіль',

  'auth.title.login': 'Вхід',
  'auth.title.register': 'Реєстрація',
  'auth.name': "Ім'я (необов'язково)",
  'auth.email': 'Email',
  'auth.password': 'Пароль (мін. 8 символів)',
  'auth.submit.login': 'Увійти',
  'auth.submit.register': 'Створити акаунт',
  'auth.switch.toRegister': 'Немає акаунта? Зареєструватися',
  'auth.switch.toLogin': 'Вже є акаунт? Увійти',
  'auth.err.login': 'Не вдалося увійти.',
  'auth.err.register': 'Не вдалося зареєструватися.',
  'auth.err.confirmManual': 'Перевірте пошту й підтвердіть акаунт, потім увійдіть.',

  'common.network': 'Не вдалося з’єднатися з сервером.',
  'common.cancel': 'Скасувати',

  'pass.entryCode': 'Код входу',
  'pass.active': 'Активна',
  'pass.location': 'Локація',
  'pass.validUntil': 'Діє до',
  'pass.price': 'Вартість',
  'pass.buyMore': 'Придбати ще',
  'pass.upcoming': 'Наступні',
  'pass.empty.title': 'Немає активного абонемента',
  'pass.empty.body': 'Придбайте абонемент на паркінг {name}, і тут з’явиться ваш QR.',
  'pass.empty.cta': 'Обрати абонемент',

  'plans.title': 'Оберіть абонемент',
  'plans.unavailable': 'Тарифи недоступні. Перевірте з’єднання й спробуйте пізніше.',
  'plans.startDate': 'Дата початку',
  'plans.checkout': 'Оформити',

  'kind.covered': 'Паркінг',
  'kind.outdoor': 'Зовнішній паркінг',
  'dur.mo1': '1 місяць',
  'dur.mo2': '2 місяці',
  'dur.mo3': '3 місяці',
  'dur.moN': '{n} міс',

  'pay.title': 'Оплата',
  'pay.amountDue': 'До сплати',
  'pay.selectMethod': 'Спосіб оплати',
  'pay.applePay': 'Apple Pay',
  'pay.appleReady': 'Touch ID · готово',
  'pay.card': 'Картка',
  'pay.cardNotSaved': 'Не зберігається після оплати',
  'pay.cardNumber': 'Номер картки',
  'pay.confirm': 'Оплатити',
  'pay.processing': 'Обробка…',
  'pay.confirmed': 'Оплачено!',
  'pay.secured': 'Захищено 256-бітним SSL-шифруванням',
  'pay.starts': 'Початок {date}',
  'pay.payWithIpay': 'Оплатити через iPay',
  'pay.ipayNote': 'Оплата на захищеній сторінці iPay. Дані картки вводяться там і не зберігаються в застосунку.',
  'pay.changeStart': 'Дата початку',
  'pay.notEarlier': 'Не раніше {date}',
  'pay.err.title': 'Оплата не пройшла',
  'pay.err.body': 'Спробуйте ще раз.',
  'pay.err.notCompleted': 'Платіж не було завершено. Спробуйте ще раз.',

  'profile.title': 'Профіль',
  'profile.vehicles': 'Автомобілі',
  'profile.swipeMore': 'гортайте',
  'profile.addCar': 'Додати авто',
  'profile.vehicle': 'Автомобіль',
  'profile.make': 'Марка',
  'profile.model': 'Модель',
  'profile.plate': 'Номерний знак',
  'profile.save': 'Зберегти',
  'profile.plateNote': 'Будь-який збережений номер розпізнається автоматично на в’їзді {name}.',
  'profile.phone': 'Телефон',
  'profile.email': 'Email',
  'profile.notSet': 'Не вказано',
  'profile.subscription': 'Абонемент',
  'profile.noActiveSub': 'Немає активного абонемента',
  'profile.settings': 'Налаштування',
  'profile.notifications': 'Сповіщення',
  'profile.appearance': 'Тема',
  'profile.language': 'Мова',
  'profile.signOut': 'Вийти',
  'profile.signOut.confirmBody': 'Вийти з акаунта на цьому пристрої?',
  'profile.build': 'Збірка {build} · {commit} · {date}',
};

const en: Dict = {
  'nav.pass': 'Pass',
  'nav.profile': 'Profile',

  'auth.title.login': 'Sign in',
  'auth.title.register': 'Sign up',
  'auth.name': 'Name (optional)',
  'auth.email': 'Email',
  'auth.password': 'Password (min. 8 characters)',
  'auth.submit.login': 'Sign in',
  'auth.submit.register': 'Create account',
  'auth.switch.toRegister': "Don't have an account? Sign up",
  'auth.switch.toLogin': 'Already have an account? Sign in',
  'auth.err.login': 'Could not sign in.',
  'auth.err.register': 'Could not sign up.',
  'auth.err.confirmManual': 'Check your email to confirm your account, then sign in.',

  'common.network': 'Could not reach the server.',
  'common.cancel': 'Cancel',

  'pass.entryCode': 'Entry code',
  'pass.active': 'Active',
  'pass.location': 'Location',
  'pass.validUntil': 'Valid until',
  'pass.price': 'Price',
  'pass.buyMore': 'Buy another',
  'pass.upcoming': 'Upcoming',
  'pass.empty.title': 'No active pass',
  'pass.empty.body': 'Buy a parking pass for {name} and your QR will appear here.',
  'pass.empty.cta': 'Choose a plan',

  'plans.title': 'Choose your plan',
  'plans.unavailable': 'Plans are unavailable. Check your connection and try again.',
  'plans.startDate': 'Start date',
  'plans.checkout': 'Checkout',

  'kind.covered': 'Parking',
  'kind.outdoor': 'Outdoor parking',
  'dur.mo1': '1 month',
  'dur.mo2': '2 months',
  'dur.mo3': '3 months',
  'dur.moN': '{n} mo',

  'pay.title': 'Payment',
  'pay.amountDue': 'Amount due',
  'pay.selectMethod': 'Select method',
  'pay.applePay': 'Apple Pay',
  'pay.appleReady': 'Touch ID · ready',
  'pay.card': 'Credit or debit card',
  'pay.cardNotSaved': 'Not saved after checkout',
  'pay.cardNumber': 'Card number',
  'pay.confirm': 'Confirm payment',
  'pay.processing': 'Processing…',
  'pay.confirmed': 'Payment confirmed!',
  'pay.secured': 'Secured by 256-bit SSL encryption',
  'pay.starts': 'Starts {date}',
  'pay.payWithIpay': 'Pay with iPay',
  'pay.ipayNote': 'Payment happens on iPay’s secure page. Card details are entered there and never stored in the app.',
  'pay.changeStart': 'Start date',
  'pay.notEarlier': 'Not earlier than {date}',
  'pay.err.notCompleted': 'The payment was not completed. Please try again.',
  'pay.err.title': 'Payment failed',
  'pay.err.body': 'Please try again.',

  'profile.title': 'Profile',
  'profile.vehicles': 'Vehicles',
  'profile.swipeMore': 'swipe for more',
  'profile.addCar': 'Add car',
  'profile.vehicle': 'Vehicle',
  'profile.make': 'Make',
  'profile.model': 'Model',
  'profile.plate': 'License plate',
  'profile.save': 'Save',
  'profile.plateNote': 'Any plate on file is recognized automatically at {name} entry.',
  'profile.phone': 'Phone',
  'profile.email': 'Email',
  'profile.notSet': 'Not set',
  'profile.subscription': 'Subscription',
  'profile.noActiveSub': 'No active pass',
  'profile.settings': 'Settings',
  'profile.notifications': 'Notifications',
  'profile.appearance': 'Appearance',
  'profile.language': 'Language',
  'profile.signOut': 'Sign out',
  'profile.signOut.confirmBody': 'Sign out of this account on this device?',
  'profile.build': 'Build {build} · {commit} · {date}',
};

export const DICTS: Record<Lang, Dict> = { uk, en };

export function translate(
  lang: Lang,
  key: string,
  vars?: Record<string, string | number>
): string {
  let s = DICTS[lang][key] ?? DICTS.uk[key] ?? key;
  if (vars) {
    for (const k of Object.keys(vars)) s = s.replace(`{${k}}`, String(vars[k]));
  }
  return s;
}

// Localized month duration, e.g. 1 -> "1 місяць" / "1 month".
export function durationLabel(months: number, t: TFunc): string {
  if (months >= 1 && months <= 3) return t(`dur.mo${months}`);
  return t('dur.moN', { n: months });
}

// Full plan label used where a single plan is shown (pass, payment).
export function planFullLabel(months: number, outdoor: boolean, t: TFunc): string {
  const d = durationLabel(months, t);
  return outdoor ? `${t('kind.outdoor')} · ${d}` : d;
}
