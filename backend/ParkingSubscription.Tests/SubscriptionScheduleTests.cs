using ParkingSubscription.Application.Payments;
using Xunit;
using Xunit.Abstractions;

namespace ParkingSubscription.Tests;

public class SubscriptionScheduleTests(ITestOutputHelper output)
{
    // Simulates a stacking chain of 1-month cards and prints each period.
    private (DateOnly start, DateOnly end) Next(
        List<(DateOnly Start, DateOnly End)> chain, DateOnly start, int days = 30)
    {
        var anchor = SubscriptionSchedule.AnchorDay(chain, start);
        var end = SubscriptionSchedule.EndDate(start, days, anchor);
        chain.Add((start, end));
        return (start, end);
    }

    [Fact]
    public void Anchor_recovers_after_february()
    {
        var chain = new List<(DateOnly, DateOnly)>();
        var start = new DateOnly(2026, 1, 31); // non-leap year
        var results = new List<(DateOnly, DateOnly)>();
        for (var i = 0; i < 6; i++)
        {
            var (s, e) = Next(chain, start);
            results.Add((s, e));
            output.WriteLine($"{s:dd.MM.yyyy} – {e:dd.MM.yyyy}");
            start = e.AddDays(1); // stacking floor
        }

        // Day-of-month returns to 31/30 whenever the month allows (no permanent 28 drift).
        Assert.Equal(new DateOnly(2026, 1, 31), results[0].Item1);
        Assert.Equal(new DateOnly(2026, 2, 27), results[0].Item2); // Jan 31 -> Feb 27 (clamped)
        Assert.Equal(new DateOnly(2026, 3, 31), results[2].Item1); // recovered to the 31st
        Assert.Equal(new DateOnly(2026, 5, 31), results[4].Item1); // and again
        // No gaps / no overlaps: each start is the previous end + 1.
        for (var i = 1; i < results.Count; i++)
            Assert.Equal(results[i - 1].Item2.AddDays(1), results[i].Item1);
    }

    [Theory]
    [InlineData(2026, "01.02.2026", "28.02.2026")] // non-leap February = 28 days
    [InlineData(2028, "01.02.2028", "29.02.2028")] // leap February = 29 days
    public void One_month_february(int _, string startStr, string expectedEnd)
    {
        var start = DateOnly.ParseExact(startStr, "dd.MM.yyyy");
        var anchor = start.Day; // fresh chain
        var end = SubscriptionSchedule.EndDate(start, 30, anchor);
        Assert.Equal(DateOnly.ParseExact(expectedEnd, "dd.MM.yyyy"), end);
    }

    [Fact]
    public void One_month_september_is_calendar_month()
    {
        var end = SubscriptionSchedule.EndDate(new DateOnly(2026, 9, 1), 30, 1);
        Assert.Equal(new DateOnly(2026, 9, 30), end);
    }
}
