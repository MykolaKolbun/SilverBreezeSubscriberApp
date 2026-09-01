namespace ParkingSubscription.Application.Payments;

/// <summary>
/// Single source of truth for subscription date math, shared by the payment flow
/// (authoritative card dates) and the period-preview API. Clients must not re-derive
/// this — they display what the backend returns.
/// </summary>
public static class SubscriptionSchedule
{
    /// <summary>
    /// Inclusive last day of a subscription that starts on <paramref name="start"/>:
    /// N whole calendar months minus one day (N = whole 30-day months in the plan),
    /// e.g. a 1-month card from 01.09 ends 30.09 and from 01.10 ends 31.10. For plans
    /// whose length is not a whole number of 30-day months, falls back to start + days - 1.
    /// </summary>
    public static DateOnly EndDate(DateOnly start, int durationDays)
    {
        var months = durationDays > 0 && durationDays % 30 == 0 ? durationDays / 30 : 0;
        return months > 0
            ? start.AddMonths(months).AddDays(-1)
            : start.AddDays(Math.Max(1, durationDays) - 1);
    }
}
