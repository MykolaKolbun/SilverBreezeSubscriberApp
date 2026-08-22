using System.Text;

namespace ParkingSubscription.Application.Common;

/// <summary>Fixed page size mandated by ТЗ §9 (50 records per page).</summary>
public static class Paging
{
    public const int PageSize = 50;
}

/// <summary>A page of results plus the opaque token to fetch the next page (ТЗ §9).</summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, string? NextPagingToken);

/// <summary>
/// Opaque <c>pagingToken</c> handling (ТЗ §9). Encodes an offset into the
/// (UpdatedAt desc) ordering as base64 so the token stays opaque to clients.
/// Offset paging is stable across providers (SQLite/PostgreSQL) and correct for
/// this facade's read workload.
/// </summary>
public static class CursorPagination
{
    public static string Encode(int nextOffset) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes($"o:{nextOffset}"));

    public static int Decode(string? pagingToken)
    {
        if (string.IsNullOrWhiteSpace(pagingToken))
            return 0;
        try
        {
            var raw = Encoding.UTF8.GetString(Convert.FromBase64String(pagingToken));
            if (raw.StartsWith("o:", StringComparison.Ordinal) &&
                int.TryParse(raw.AsSpan(2), out var offset) && offset >= 0)
            {
                return offset;
            }
        }
        catch (FormatException)
        {
            // fall through to validation error below
        }
        throw new ValidationException("Invalid pagingToken.");
    }
}
