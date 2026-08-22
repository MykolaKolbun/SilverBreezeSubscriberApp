using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ParkingSubscription.Application.Common;
using ParkingSubscription.Application.Facade;
using ParkingSubscription.Domain.Entities;
using ParkingSubscription.Infrastructure.Persistence;
using Xunit;

namespace ParkingSubscription.Tests;

public sealed class PaginationTests
{
    [Fact]
    public void Cursor_roundtrips_offset()
    {
        var token = CursorPagination.Encode(50);
        Assert.Equal(50, CursorPagination.Decode(token));
        Assert.Equal(0, CursorPagination.Decode(null));
    }

    [Fact]
    public void Invalid_paging_token_throws_validation()
    {
        Assert.Throws<ValidationException>(() => CursorPagination.Decode("not-base64-@@@"));
    }

    [Fact]
    public async Task Pages_are_50_records_with_next_token()
    {
        await using var ctx = NewContext(out var connection);
        using var _ = connection;

        for (var i = 0; i < 51; i++)
            ctx.Customers.Add(new Customer { Name = $"c{i}", UpdatedAt = DateTimeOffset.UtcNow.AddSeconds(i) });
        await ctx.SaveChangesAsync();

        var page1 = await ctx.Customers.ToPagedAsync(null, Mapping.ToDto);
        Assert.Equal(50, page1.Items.Count);
        Assert.NotNull(page1.NextPagingToken);

        var page2 = await ctx.Customers.ToPagedAsync(page1.NextPagingToken, Mapping.ToDto);
        Assert.Single(page2.Items);
        Assert.Null(page2.NextPagingToken);
    }

    private static AppDbContext NewContext(out SqliteConnection connection)
    {
        connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        var ctx = new AppDbContext(options);
        ctx.Database.EnsureCreated();
        return ctx;
    }
}
