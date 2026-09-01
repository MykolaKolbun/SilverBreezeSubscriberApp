namespace ParkingSubscription.Web;

/// <summary>
/// Tiny two-language (uk/en) text service. The active language comes from the
/// "sb_lang" cookie (default uk). Injected into views as <c>L</c>.
/// </summary>
public sealed class LocService(IHttpContextAccessor accessor)
{
    public string Lang =>
        accessor.HttpContext?.Request.Cookies["sb_lang"] == "en" ? "en" : "uk";

    public string this[string key] =>
        Text.TryGetValue(key, out var v) ? (Lang == "en" ? v.En : v.Uk) : key;

    public string Format(string key, params object[] args) => string.Format(this[key], args);

    public string Status(string code) => code.ToLowerInvariant() switch
    {
        "active" => this["st.active"],
        "pending" => this["st.pending"],
        "suspended" => this["st.suspended"],
        "blocked" => this["st.blocked"],
        "expired" => this["st.expired"],
        "deleted" => this["st.deleted"],
        "succeeded" => this["st.succeeded"],
        "declined" => this["st.declined"],
        "failed" => this["st.failed"],
        "timedout" => this["st.timedout"],
        "refunded" => this["st.refunded"],
        _ => code
    };

    private static readonly Dictionary<string, (string Uk, string En)> Text = new()
    {
        // Nav / layout
        ["nav.plans"] = ("Тарифи", "Plans"),
        ["nav.cards"] = ("Мої пропуски", "My passes"),
        ["nav.history"] = ("Історія", "History"),
        ["nav.profile"] = ("Профіль", "Profile"),
        ["nav.logout"] = ("Вийти", "Sign out"),
        ["nav.login"] = ("Увійти", "Sign in"),
        ["footer"] = ("SilverBreeze · контрактний паркінг · Київ, вул. Павла Тичини 1В",
                      "SilverBreeze · contract parking · Kyiv, 1V Pavla Tychyny St"),
        ["theme.aria"] = ("Змінити тему", "Toggle theme"),

        // Common
        ["common.email"] = ("Email", "Email"),
        ["common.save"] = ("Зберегти", "Save"),
        ["common.saved"] = ("Збережено.", "Saved."),

        // Login
        ["login.title"] = ("Вхід", "Sign in"),
        ["login.getCode"] = ("Отримати код", "Get code"),
        ["login.hint"] = ("Ми надішлемо одноразовий код на пошту — пароль не потрібен.",
                          "We'll email you a one-time code — no password needed."),
        ["login.codeLabel"] = ("Код із листа", "Code from the email"),
        ["login.signIn"] = ("Увійти", "Sign in"),
        ["login.sentTo"] = ("Код надіслано на", "Code sent to"),
        ["login.devCode"] = ("Режим розробки: код підставлено автоматично.",
                             "Dev mode: the code is prefilled."),
        ["login.resend"] = ("Надіслати код ще раз", "Resend code"),
        ["err.needEmail"] = ("Вкажіть email.", "Enter your email."),
        ["err.needCode"] = ("Введіть код із листа.", "Enter the code from the email."),

        // Index / plans
        ["index.subsTitle"] = ("Абонементи на паркінг", "Parking subscriptions"),
        ["index.leadIntro"] = ("Придбайте контрактний паркувальний абонемент, отримайте QR-код і пропуск у гаманець.",
                               "Buy a contract parking subscription, get a QR code and a wallet pass."),
        ["index.leadLogin"] = ("Увійдіть за email", "Sign in with email"),
        ["index.leadTail"] = (", щоб побачити тарифи — пароль не потрібен.",
                              " to see the plans — no password needed."),
        ["index.noPlans"] = ("Наразі активних тарифів немає.", "No active plans right now."),
        ["index.choose"] = ("Оберіть абонемент", "Choose a subscription"),
        ["index.venue"] = ("Паркінг SilverBreeze · Київ, вул. Павла Тичини 1В",
                           "SilverBreeze parking · Kyiv, 1V Pavla Tychyny St"),
        ["plan.perDay"] = ("/ день", "/ day"),
        ["plan.valid"] = ("Діє {0} днів", "Valid for {0} days"),
        ["plan.buy"] = ("Придбати", "Buy"),

        // Buy
        ["buy.title"] = ("Оформлення", "Checkout"),
        ["buy.h1"] = ("Оформлення абонемента", "Subscription checkout"),
        ["buy.startLabel"] = ("Дата початку", "Start date"),
        ["buy.notBefore"] = ("Не раніше {0} — новий абонемент не може перетинатися з чинним.",
                             "No earlier than {0} — a new subscription can't overlap the current one."),
        ["buy.period"] = ("Період", "Period"),
        ["buy.toPay"] = ("Перейти до оплати", "Proceed to payment"),
        ["buy.back"] = ("← До тарифів", "← Back to plans"),

        // Pay
        ["pay.title"] = ("Оплата", "Payment"),
        ["pay.amount"] = ("Сума", "Amount"),
        ["pay.status"] = ("Статус", "Status"),
        ["pay.done"] = ("Оплату завершено — ваш абонемент активний.",
                        "Payment complete — your subscription is active."),
        ["pay.receipt"] = ("Фіскальний чек", "Fiscal receipt"),
        ["pay.toPass"] = ("Перейти до мого пропуску →", "Go to my pass →"),
        ["pay.processing"] = ("Оплата ще обробляється. Оновіть статус за кілька секунд.",
                              "Payment is still processing. Refresh in a few seconds."),
        ["pay.refresh"] = ("Оновити статус", "Refresh status"),
        ["pay.failed"] = ("Оплату не завершено.", "Payment was not completed."),
        ["pay.choose"] = ("Обрати абонемент", "Choose a subscription"),

        // Cards / pass
        ["cards.title"] = ("Мої пропуски", "My passes"),
        ["cards.emptyIntro"] = ("У вас ще немає паркувального пропуску.", "You don't have a parking pass yet."),
        ["cards.emptyLink"] = ("Придбайте абонемент", "Buy a subscription"),
        ["cards.emptyTail"] = (", щоб його отримати.", " to get one."),
        ["pass.tag"] = ("Паркувальний пропуск", "Parking pass"),
        ["pass.showQr"] = ("Покажіть цей QR на шлагбаумі", "Show this QR at the barrier"),
        ["pass.from"] = ("Діє з", "Valid from"),
        ["pass.to"] = ("Діє до", "Valid to"),
        ["pass.noCurrent"] = ("Немає активного пропуску на сьогодні.", "No active pass for today."),
        ["cards.others"] = ("Інші абонементи", "Other subscriptions"),
        ["cards.startsOn"] = ("розпочнеться {0}", "starts {0}"),
        ["cards.ended"] = ("завершено", "ended"),
        ["cards.planned"] = ("Заплановано", "Scheduled"),
        ["cards.finished"] = ("Завершено", "Finished"),
        ["cards.renew"] = ("Продовжити абонемент", "Renew subscription"),

        // Profile
        ["profile.title"] = ("Профіль", "Profile"),
        ["profile.incomplete"] = ("Заповніть ім'я та прізвище — без цього не можна придбати абонемент.",
                                  "Fill in your name and surname — required to buy a subscription."),
        ["profile.contact"] = ("Контактні дані", "Contact details"),
        ["profile.firstName"] = ("Ім'я", "First name"),
        ["profile.surname"] = ("Прізвище", "Surname"),
        ["profile.phone"] = ("Телефон", "Phone"),
        ["profile.phoneHint"] = ("Телефон не обов'язковий, але бажаний.", "Phone is optional but recommended."),
        ["profile.vehicles"] = ("Автомобілі", "Vehicles"),
        ["profile.noVehicles"] = ("Ще не додано. Авто потрібне для в'їзду за номером.",
                                  "None yet. A vehicle is needed for plate-based entry."),
        ["profile.plate"] = ("Номер авто", "Plate number"),
        ["profile.make"] = ("Марка", "Make"),
        ["profile.model"] = ("Модель", "Model"),
        ["profile.addVehicle"] = ("Додати авто", "Add vehicle"),
        ["profile.delete"] = ("Видалити", "Delete"),
        ["profile.limit"] = ("Досягнуто ліміту ({0}).", "Limit reached ({0})."),
        ["err.needPlate"] = ("Вкажіть номер авто.", "Enter the plate number."),

        // History
        ["history.title"] = ("Історія платежів", "Payment history"),
        ["history.empty"] = ("Платежів ще немає.", "No payments yet."),
        ["history.receipt"] = ("Чек", "Receipt"),
        ["history.pdf"] = ("PDF", "PDF"),

        // Statuses
        ["st.active"] = ("Активний", "Active"),
        ["st.pending"] = ("Очікує", "Pending"),
        ["st.suspended"] = ("Призупинено", "Suspended"),
        ["st.blocked"] = ("Заблоковано", "Blocked"),
        ["st.expired"] = ("Прострочено", "Expired"),
        ["st.deleted"] = ("Видалено", "Deleted"),
        ["st.succeeded"] = ("Успішно", "Succeeded"),
        ["st.declined"] = ("Відхилено", "Declined"),
        ["st.failed"] = ("Помилка", "Failed"),
        ["st.timedout"] = ("Час вичерпано", "Timed out"),
        ["st.refunded"] = ("Повернено", "Refunded"),
    };
}
