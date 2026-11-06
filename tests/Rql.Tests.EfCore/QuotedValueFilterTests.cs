using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mpt.Rql;
using Xunit;

namespace Rql.Tests.EfCore;

/// <summary>
/// Quoted filter values that hold their own delimiter as a doubled pair, translated and executed by EF Core (SQLite in memory).
/// The pair must reach the database as one literal delimiter, so the filter selects the row holding the value as stored.
/// </summary>
public sealed class QuotedValueFilterTests : IDisposable
{
    private static readonly IRqlQueryable<Order, OrderView> Rql = new ServiceCollection()
        .AddRql(rql => rql.ScanForMappers(typeof(QuotedValueFilterTests).Assembly))
        .BuildServiceProvider()
        .GetRequiredService<IRqlQueryable<Order, OrderView>>();

    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly OrdersDbContext _context;

    public QuotedValueFilterTests()
    {
        _connection.Open();
        _context = new OrdersDbContext(new DbContextOptionsBuilder<OrdersDbContext>().UseSqlite(_connection).Options);
        _context.Database.EnsureCreated();

        // order 1 holds the value the filters target, order 2 the same text with the pair kept, order 3 the text after the first quote
        _context.Orders.AddRange(
            new Order { Id = 1, Number = "it's \"quoted\"" },
            new Order { Id = 2, Number = "it''s \"quoted\"" },
            new Order { Id = 3, Number = "s \"quoted\"" },
            new Order { Id = 4, Number = "O4" });
        _context.SaveChanges();
        _context.ChangeTracker.Clear();
    }

    public static TheoryData<string, int[]> Filters => new()
    {
        { "eq(number,'it''s \"quoted\"')", [1] },
        { "ilike(number,'*it''s \"q*')", [1] },
    };

    [Theory]
    [MemberData(nameof(Filters))]
    public void Filter_QuotedValueWithDoubledDelimiter_SelectsTheRowHoldingOneDelimiter(string filter, int[] expected)
    {
        Transform(_context.Orders, filter).Select(o => o.Id).ToList().Should().Equal(expected);
    }

    private static IQueryable<OrderView> Transform(IQueryable<Order> source, string filter)
    {
        var response = Rql.Transform(source, new RqlRequest { Filter = filter, Order = "id" });

        response.IsSuccess.Should().BeTrue(string.Join("; ", response.Errors.Select(e => e.Message)));
        return response.Query;
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
