using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mpt.Rql;
using Xunit;

namespace Rql.Tests.EfCore;

/// <summary>
/// A computed view property declaring <see cref="RqlPropertyAttribute.DependsOn"/>, translated and executed by EF Core (SQLite in memory).
/// Projecting <see cref="ComputedParameterView.Value"/> must read the columns its getter needs and no unrelated one,
/// projecting anything else must not read them, and the rows returned must carry the value the getter computes from the stored inputs.
/// </summary>
public sealed class ComputedPropertyDependencyTests : IDisposable
{
    private static readonly IRqlQueryable<ComputedSubscription, ComputedSubscriptionView> Rql = new ServiceCollection()
        .AddRql(rql =>
        {
            rql.ScanForMappers(typeof(ComputedPropertyDependencyTests).Assembly);
            rql.Settings.Select.Implicit = RqlSelectModes.Core | RqlSelectModes.Primitive | RqlSelectModes.Reference;
            rql.Settings.Select.Explicit = RqlSelectModes.All;
            rql.Settings.Select.MaxDepth = 10;
        })
        .BuildServiceProvider()
        .GetRequiredService<IRqlQueryable<ComputedSubscription, ComputedSubscriptionView>>();

    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly ComputedSubscriptionsDbContext _context;

    public ComputedPropertyDependencyTests()
    {
        _connection.Open();
        _context = new ComputedSubscriptionsDbContext(new DbContextOptionsBuilder<ComputedSubscriptionsDbContext>().UseSqlite(_connection).Options);
        _context.Database.EnsureCreated();

        // parameter 1 is the row whose format changes the value, parameters 2 and 3 keep the raw value as stored
        _context.Subscriptions.AddRange(
            new ComputedSubscription
            {
                Id = 1,
                Name = "S1",
                Parameters =
                [
                    new ComputedSubscriptionParameter { Id = 1, ExternalId = "P1", RawValue = "abc", Format = "upper", DisplayValue = "large display 1" },
                    new ComputedSubscriptionParameter { Id = 2, ExternalId = "P2", RawValue = "def", Format = "plain", DisplayValue = "large display 2" },
                ],
            },
            new ComputedSubscription
            {
                Id = 2,
                Name = "S2",
                Parameters = [new ComputedSubscriptionParameter { Id = 3, ExternalId = "P3", RawValue = "Xyz", DisplayValue = "large display 3" }],
            });
        _context.SaveChanges();
        _context.ChangeTracker.Clear();
    }

    [Fact]
    public void Select_ComputedProperty_ReadsItsDependencyColumnsAndNoUnrelatedOne()
    {
        var sql = Transform(_context.Subscriptions, "-*,id,parameters.value").ToQueryString();

        var alias = ParameterTableAlias(sql);
        sql.Should().Contain($"\"{alias}\".\"RawValue\"", sql);
        sql.Should().Contain($"\"{alias}\".\"Format\"", sql);
        sql.Should().NotContain("\"DisplayValue\"", sql);
    }

    [Fact]
    public void Select_PropertyWithoutDependencies_ReadsNoneOfTheComputedPropertysDependencies()
    {
        var sql = Transform(_context.Subscriptions, "-*,id,parameters.externalId").ToQueryString();

        var alias = ParameterTableAlias(sql);
        sql.Should().Contain($"\"{alias}\".\"ExternalId\"", sql);
        sql.Should().NotContain("\"RawValue\"", sql);
        sql.Should().NotContain("\"Format\"", sql);
    }

    [Fact]
    public void Select_ComputedProperty_ReturnsTheValueComputedFromTheStoredInputs()
    {
        var subscriptions = Transform(_context.Subscriptions, "-*,id,parameters.value").ToList();

        // expected values: the seeded RawValue, upper-cased where the seeded Format is "upper"
        subscriptions.Select(s => s.Id).Should().Equal(1, 2);
        subscriptions[0].Parameters!.Select(p => p.Value).Should().Equal("ABC", "def");
        subscriptions[1].Parameters!.Select(p => p.Value).Should().Equal("Xyz");
    }

    [Fact]
    public void Select_ComputedPropertyWithNonSelectableDependencyDeselected_StillReturnsTheComputedValue()
    {
        var subscriptions = Transform(_context.Subscriptions, "-*,id,parameters.value,-parameters.format").ToList();

        // expected values: the seeded RawValue, upper-cased where the seeded Format is "upper"
        subscriptions.Select(s => s.Id).Should().Equal(1, 2);
        subscriptions[0].Parameters!.Select(p => p.Value).Should().Equal("ABC", "def");
        subscriptions[1].Parameters!.Select(p => p.Value).Should().Equal("Xyz");
    }

    private static IQueryable<ComputedSubscriptionView> Transform(IQueryable<ComputedSubscription> source, string select)
    {
        var response = Rql.Transform(source, new RqlRequest { Select = select, Order = "id" });

        response.IsSuccess.Should().BeTrue(string.Join("; ", response.Errors.Select(e => e.Message)));
        return response.Query;
    }

    // the alias EF Core gives the parameter table in the join, read from the query itself
    private static string ParameterTableAlias(string sql)
    {
        const string table = "\"ComputedSubscriptionParameter\" AS \"";
        var start = sql.IndexOf(table, StringComparison.Ordinal);
        start.Should().BeGreaterThan(0, sql);

        start += table.Length;
        return sql[start..sql.IndexOf('"', start)];
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
