using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using ParkingSubscription.Application.Abstractions;
using ParkingSubscription.Domain.Entities;
using ParkingSubscription.Infrastructure.Persistence;

namespace ParkingSubscription.AdminPanel.Pages;

public sealed class SettingsModel(AppDbContext db, ICredentialProtector protector) : PageModel
{
    // Fiscal (Checkbox)
    [BindProperty] public string? FiscalBaseUrl { get; set; }
    [BindProperty] public string? FiscalTaxCode { get; set; }
    [BindProperty] public string? FiscalPin { get; set; }
    [BindProperty] public string? FiscalLicense { get; set; }
    public bool FiscalHasPin { get; private set; }
    public bool FiscalHasLicense { get; private set; }

    // iPay
    [BindProperty] public string? IpayMerchantId { get; set; }
    [BindProperty] public string? IpayBaseUrl { get; set; }
    [BindProperty] public string? IpaySignKey { get; set; }
    public bool IpayHasSignKey { get; private set; }

    // Parking capacity
    [BindProperty] public int? ParkingCapacity { get; set; }

    // SKIDATA sweb parking integration
    [BindProperty] public bool SkidataEnabled { get; set; }
    [BindProperty] public string? SkidataBaseUrl { get; set; }
    [BindProperty] public string? SkidataUsername { get; set; }
    [BindProperty] public string? SkidataPassword { get; set; }
    [BindProperty] public string? SkidataFacilityNumber { get; set; }
    public bool SkidataHasUsername { get; private set; }
    public bool SkidataHasPassword { get; private set; }

    public string? Saved { get; private set; }

    public async Task OnGetAsync(CancellationToken ct) => await LoadAsync(ct);

    private async Task LoadAsync(CancellationToken ct)
    {
        var fiscal = await db.FiscalGatewayConfigs.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == FiscalGatewayConfig.SingletonId, ct);
        FiscalBaseUrl = fiscal?.BaseUrl ?? "https://api.checkbox.ua";
        FiscalTaxCode = fiscal?.TaxCode?.ToString();
        FiscalHasPin = !string.IsNullOrEmpty(fiscal?.PinCodeEncrypted);
        FiscalHasLicense = !string.IsNullOrEmpty(fiscal?.LicenseKeyEncrypted);

        var ipay = await db.PaymentGatewayConfigs.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == PaymentGatewayConfig.SingletonId, ct);
        IpayMerchantId = ipay?.MerchantId;
        IpayBaseUrl = ipay?.BaseUrl ?? "https://sandbox-checkout.ipay.ua/api302";
        IpayHasSignKey = !string.IsNullOrEmpty(ipay?.SignKeyEncrypted);

        ParkingCapacity = await db.AdminConfigs.AsNoTracking()
            .Select(a => a.MaxActiveSubscriptions).FirstOrDefaultAsync(ct);

        var sk = await db.ParkingIntegrationConfigs.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == ParkingIntegrationConfig.SingletonId, ct);
        SkidataEnabled = sk?.Enabled ?? false;
        SkidataBaseUrl = sk?.BaseUrl;
        SkidataFacilityNumber = sk?.FacilityNumber;
        SkidataHasUsername = !string.IsNullOrEmpty(sk?.UsernameEncrypted);
        SkidataHasPassword = !string.IsNullOrEmpty(sk?.PasswordEncrypted);
    }

    public async Task<IActionResult> OnPostFiscalAsync(CancellationToken ct)
    {
        var row = await db.FiscalGatewayConfigs.FindAsync([FiscalGatewayConfig.SingletonId], ct)
                  ?? db.FiscalGatewayConfigs.Add(new FiscalGatewayConfig()).Entity;
        row.BaseUrl = string.IsNullOrWhiteSpace(FiscalBaseUrl) ? row.BaseUrl : FiscalBaseUrl.Trim();
        row.TaxCode = int.TryParse(FiscalTaxCode, out var tc) ? tc : null;
        if (!string.IsNullOrWhiteSpace(FiscalPin)) row.PinCodeEncrypted = protector.Protect(FiscalPin.Trim());
        if (!string.IsNullOrWhiteSpace(FiscalLicense)) row.LicenseKeyEncrypted = protector.Protect(FiscalLicense.Trim());
        row.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        Saved = "fiscal";
        await LoadAsync(ct);
        return Page();
    }

    public async Task<IActionResult> OnPostIpayAsync(CancellationToken ct)
    {
        var row = await db.PaymentGatewayConfigs.FindAsync([PaymentGatewayConfig.SingletonId], ct)
                  ?? db.PaymentGatewayConfigs.Add(new PaymentGatewayConfig()).Entity;
        row.MerchantId = string.IsNullOrWhiteSpace(IpayMerchantId) ? row.MerchantId : IpayMerchantId.Trim();
        row.BaseUrl = string.IsNullOrWhiteSpace(IpayBaseUrl) ? row.BaseUrl : IpayBaseUrl.Trim();
        if (!string.IsNullOrWhiteSpace(IpaySignKey)) row.SignKeyEncrypted = protector.Protect(IpaySignKey.Trim());
        row.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        Saved = "ipay";
        await LoadAsync(ct);
        return Page();
    }

    public async Task<IActionResult> OnPostParkingAsync(CancellationToken ct)
    {
        var row = await db.AdminConfigs.FirstOrDefaultAsync(a => a.Id == AdminConfig.SingletonId, ct);
        if (row is not null)
        {
            row.MaxActiveSubscriptions = ParkingCapacity is > 0 ? ParkingCapacity : null;
            row.UpdatedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
        }
        Saved = "parking";
        await LoadAsync(ct);
        return Page();
    }

    public async Task<IActionResult> OnPostSkidataAsync(CancellationToken ct)
    {
        var row = await db.ParkingIntegrationConfigs.FindAsync([ParkingIntegrationConfig.SingletonId], ct)
                  ?? db.ParkingIntegrationConfigs.Add(new ParkingIntegrationConfig()).Entity;
        row.Enabled = SkidataEnabled;
        row.BaseUrl = string.IsNullOrWhiteSpace(SkidataBaseUrl) ? row.BaseUrl : SkidataBaseUrl.Trim();
        row.FacilityNumber = string.IsNullOrWhiteSpace(SkidataFacilityNumber) ? row.FacilityNumber : SkidataFacilityNumber.Trim();
        if (!string.IsNullOrWhiteSpace(SkidataUsername)) row.UsernameEncrypted = protector.Protect(SkidataUsername.Trim());
        if (!string.IsNullOrWhiteSpace(SkidataPassword)) row.PasswordEncrypted = protector.Protect(SkidataPassword.Trim());
        row.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        Saved = "skidata";
        await LoadAsync(ct);
        return Page();
    }
}
