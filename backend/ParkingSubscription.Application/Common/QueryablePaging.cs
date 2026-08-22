using Microsoft.EntityFrameworkCore;
using ParkingSubscription.Domain.Common;

namespace ParkingSubscription.Application.Common;

public static class QueryablePaging
{
    /// <summary>
    /// Applies offset pagination ordered by (UpdatedAt desc, Id desc) using an
    /// opaque <paramref name="pagingToken"/>, fixed page size 50 (ТЗ §9), and
    /// projects each entity via <paramref name="map"/>.
    /// </summary>
    public static async Task<PagedResult<TDto>> ToPagedAsync<TEntity, TDto>(
        this IQueryable<TEntity> source,
        string? pagingToken,
        Func<TEntity, TDto> map,
        CancellationToken ct = default)
        where TEntity : Entity
    {
        var offset = CursorPagination.Decode(pagingToken);

        var rows = await source
            .OrderByDescending(e => e.UpdatedAt).ThenByDescending(e => e.Id)
            .Skip(offset)
            .Take(Paging.PageSize + 1)
            .ToListAsync(ct);

        string? next = null;
        if (rows.Count > Paging.PageSize)
        {
            next = CursorPagination.Encode(offset + Paging.PageSize);
            rows.RemoveAt(rows.Count - 1);
        }

        return new PagedResult<TDto>(rows.Select(map).ToList(), next);
    }
}
