namespace ParkingSubscription.Application.Payments;

/// <summary>
/// URLs used by the redirect payment flow (bound from the "Payment" config section).
///
/// <see cref="PublicBaseUrl"/> is the internet-reachable base of THIS API — the
/// provider redirects the user's browser back to <c>{PublicBaseUrl}/payments/resolve</c>.
/// <see cref="AppReturnUrl"/> is the mobile deep link the resolve endpoint bounces the
/// browser to once the payment has been confirmed server-side.
/// </summary>
public sealed class PaymentUrlOptions
{
    public const string SectionName = "Payment";

    public string PublicBaseUrl { get; set; } = "http://localhost:8080";
    public string AppReturnUrl { get; set; } = "silverbreeze://payment";

    /// <summary>
    /// Where the resolve endpoint sends the browser for WEB clients (client=web) once the
    /// payment is confirmed — the public URL of the Web client's payment-result page.
    /// The mobile app keeps using <see cref="AppReturnUrl"/> (the deep link).
    /// </summary>
    public string WebReturnUrl { get; set; } = "http://localhost:5100/Pay";
}
