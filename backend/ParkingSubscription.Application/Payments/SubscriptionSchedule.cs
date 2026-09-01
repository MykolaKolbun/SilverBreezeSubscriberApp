namespace ParkingSubscription.Application.Payments;

/// <summary>
/// Single source of truth for subscription date math, shared by the payment flow
/// (authoritative card dates) and the period-preview API. Clients must not re-derive
/// this — they display what the backend returns.
///
/// Monthly periods are anchored to the day-of-month of the chain's original start, so
/// end-of-month starts self-correct after short months (e.g. 31.01 → 27.02 → 30.03 →
/// 29.04 → 30.05…, returning to the 31st/30th whenever the month has that day) instead
/// of drifting to the 28th forever.
/// </summary>
public static class SubscriptionSchedule
{
    /// <summary>
    /// Inclusive last day of a subscription that starts on <paramref name="start"/> and lasts
    /// N whole calendar months (N = whole 30-day months in the plan). The next period boundary
    /// is snapped to <paramref name="anchorDay"/> of the target month (clamped to that month's
    /// length), and the end is the day before it. For plans that are not a whole number of
    /// 30-day months, falls back to start + days - 1.
    /// </summary>
    public static DateOnly EndDate(DateOnly start, int durationDays, int anchorDay)
    {
        var months = durationDays > 0 && durationDays % 30 == 0 ? durationDays / 30 : 0;
        if (months <= 0)
            return start.AddDays(Math.Max(1, durationDays) - 1);

        var target = start.AddMonths(months); // correct target month (day may be clamped)
        var day = Math.Min(anchorDay, DateTime.DaysInMonth(target.Year, target.Month));
        var boundary = new DateOnly(target.Year, target.Month, day);
        return boundary.AddDays(-1);
    }

    /// <summary>
    /// The anchor day-of-month for a card starting on <paramref name="start"/>: the day-of-month
    /// of the earliest card in the contiguous active chain that ends the day before
    /// <paramref name="start"/>. If the start begins a fresh chain (no adjacent predecessor),
    /// the anchor is the start's own day. This preserves the original day-of-month across
    /// renewals even after a short-month clamp.
    /// </summary>
    public static int AnchorDay(IEnumerable<(DateOnly Start, DateOnly End)> activeCards, DateOnly start)
    {
        // end-date -> start-date, to walk predecessors (active periods don't overlap).
        var startByEnd = new Dictionary<DateOnly, DateOnly>();
        foreach (var c in activeCards)
            startByEnd[c.End] = c.Start;

        var anchorStart = start;
        var guard = 0;
        while (startByEnd.TryGetValue(anchorStart.AddDays(-1), out var predStart) && guard++ < 600)
            anchorStart = predStart;

        return anchorStart.Day;
    }
}
