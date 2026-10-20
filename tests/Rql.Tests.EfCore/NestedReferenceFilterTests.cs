using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mpt.Rql;
using Mpt.Rql.Abstractions.Configuration;
using Mpt.Rql.Abstractions.Configuration.Filter;
using Xunit;

namespace Rql.Tests.EfCore;

/// <summary>
/// Filters through nullable nested references, translated and executed by EF Core (SQLite in memory). The projection
/// guards every nullable reference (<c>x != null ? new T { ... } : null</c>); with
/// <see cref="FilterPlacement.BeforeProjection"/> a filter through such references must reach the database as a plain
/// column predicate, so it can use an index, and select the same rows as the default placement.
/// </summary>
public sealed class NestedReferenceFilterTests : IDisposable
{
    private static readonly IRqlQueryable<Order, OrderView> Rql = new ServiceCollection()
        .AddRql(rql =>
        {
            rql.ScanForMappers(typeof(NestedReferenceFilterTests).Assembly);
            rql.Settings.Select.Implicit = RqlSelectModes.Core | RqlSelectModes.Primitive | RqlSelectModes.Reference;
            rql.Settings.Select.Explicit = RqlSelectModes.All;
            rql.Settings.Select.MaxDepth = 10;
        })
        .BuildServiceProvider()
        .GetRequiredService<IRqlQueryable<Order, OrderView>>();

    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly OrdersDbContext _context;

    public NestedReferenceFilterTests()
    {
        _connection.Open();
        _context = new OrdersDbContext(new DbContextOptionsBuilder<OrdersDbContext>().UseSqlite(_connection).Options);
        _context.Database.EnsureCreated();

        _context.Orders.AddRange(
            new Order { Id = 1, Number = "O1", Customer = new Customer { Id = 10, Status = "Active", Contact = new Person { Id = 100, Email = "ann@x.io", Phone = "1", Employer = new Employer { Id = 1000, Name = "Acme" } } }, Lines = [new OrderLine { Id = 1, Sku = "ABC" }] },
            new Order { Id = 2, Number = "O2", Customer = new Customer { Id = 20, Status = "Blocked", Contact = new Person { Id = 200, Email = "bob@x.io" } } },
            new Order { Id = 3, Number = "O3", Customer = new Customer { Id = 30, Status = "Active" }, Lines = [new OrderLine { Id = 2, Sku = "XYZ" }] },
            new Order { Id = 4, Number = "O4" });
        _context.SaveChanges();
        _context.ChangeTracker.Clear();
    }

    public static TheoryData<string, int[]> Filters => new()
    {
        { "eq(customer.contact.email,ann@x.io)", [1] },
        { "ne(customer.contact.email,ann@x.io)", [2, 3, 4] },
        { "eq(customer.contact.email,null())", [3, 4] },
        { "ne(customer.contact.email,null())", [1, 2] },
        { "eq(customer.contact.phone,null())", [2, 3, 4] },
        { "like(customer.contact.email,*@x.io)", [1, 2] },
        { "in(customer.contact.email,(ann@x.io,bob@x.io))", [1, 2] },
        { "out(customer.contact.email,(ann@x.io,bob@x.io))", [3, 4] },
        { "eq(customer.contact.id,200)", [2] },
        { "gt(customer.contact.id,150)", [2] },
        { "eq(customer.status,Active)", [1, 3] },
        { "gt(customer.id,15)", [2, 3] },
        { "out(customer.id,(10,20))", [3, 4] },
        { "eq(customer.contact.label,none)", [2] },
        { "eq(customer.contact.mobile,1)", [1] },
        { "eq(customer.contact.mobile,null())", [2, 3, 4] },
        { "eq(customer.contact.employerName,Acme)", [1] },
        { "eq(customer.contact.employerName,null())", [2, 3, 4] },
        { "and(eq(customer.contact.email,bob@x.io),eq(customer.contact.label,none))", [2] },
        { "or(eq(customer.contact.mobile,1),eq(customer.contact.label,none))", [1, 2] },
        { "and(eq(customer.status,Active),ne(customer.contact.email,null()))", [1] },
        { "or(eq(customer.contact.email,bob@x.io),eq(number,O4))", [2, 4] },
        { "not(eq(customer.contact.email,ann@x.io))", [2, 3, 4] },
        { "any(lines,eq(sku,ABC))", [1] },
    };

    [Theory]
    [MemberData(nameof(Filters))]
    public void Filter_SelectsExpectedRows_InEitherPlacement(string filter, int[] expected)
    {
        foreach (var placement in Enum.GetValues<FilterPlacement>())
            Transform(_context.Orders, filter, placement: placement).Select(o => o.Id).ToList().Should().Equal(expected, "with {0}", placement);
    }

    // EF cannot translate safe navigation over a collection: the projection guards the list itself
    public static IEnumerable<object[]> ReferenceFilters => Filters.Where(row => !((string)row[0]).StartsWith("any(", StringComparison.Ordinal));

    [Theory]
    [MemberData(nameof(ReferenceFilters))]
    public void Filter_WithSafeNavigation_SelectsTheRowsLinqToObjectsSelects(string filter, int[] _)
    {
        // Safe navigation has its own null semantics, so the reference is the same request evaluated in memory, on the view.
        var loaded = _context.Orders.Include(o => o.Customer!).ThenInclude(c => c.Contact!).ThenInclude(p => p.Employer).Include(o => o.Lines).AsNoTracking().ToList();
        var expected = Transform(loaded.AsQueryable(), filter, NavigationStrategy.Safe, FilterPlacement.AfterProjection).Select(o => o.Id).ToList();

        Transform(_context.Orders, filter, NavigationStrategy.Safe).Select(o => o.Id).ToList().Should().Equal(expected);
    }

    [Theory]
    [InlineData("eq(customer.contact.email,ann@x.io)", "\"p\".\"Email\" = @")]
    [InlineData("ne(customer.contact.email,ann@x.io)", "\"p\".\"Email\" <> @")]
    [InlineData("eq(customer.contact.email,null())", "\"p\".\"Email\" IS NULL")]
    [InlineData("like(customer.contact.email,*@x.io)", "\"p\".\"Email\"")]
    [InlineData("in(customer.contact.email,(ann@x.io,bob@x.io))", "\"p\".\"Email\" IN (")]
    [InlineData("out(customer.contact.email,(ann@x.io,bob@x.io))", "\"p\".\"Email\" NOT IN (")]
    [InlineData("eq(customer.contact.id,200)", "\"p\".\"Id\" = @")]
    [InlineData("gt(customer.contact.id,150)", "\"p\".\"Id\" > @")]
    [InlineData("eq(customer.status,Active)", "\"c\".\"Status\" = @")]
    [InlineData("gt(customer.id,15)", "\"c\".\"Id\" > @")]
    [InlineData("and(eq(customer.status,Active),ne(customer.contact.email,null()))", "\"p\".\"Email\" IS NOT NULL")]
    [InlineData("eq(customer.contact.mobile,1)", "\"p\".\"Phone\" = @")]
    [InlineData("eq(customer.contact.employerName,Acme)", "\"e\".\"Name\" = @")]
    public void Filter_ThroughNullableReferences_IsAPlainColumnPredicate(string filter, string predicate)
    {
        var sql = Transform(_context.Orders, filter).ToQueryString();

        var where = Where(sql);
        where.Should().NotContain("CASE", sql);
        where.Should().Contain(predicate, sql);
    }

    [Fact]
    public void Filter_AfterProjection_LeavesTheNullGuardAroundTheColumn()
    {
        // the default placement, as before: EF Core keeps the outer reference's guard as a CASE around the column
        var sql = Transform(_context.Orders, "eq(customer.contact.email,ann@x.io)", placement: FilterPlacement.AfterProjection).ToQueryString();

        Where(sql).Should().Contain("CASE", sql);
    }

    [Fact]
    public void Filter_ThroughComputedMember_KeepsTheNullGuard()
    {
        // `Phone ?? "none"` is not null when the person is missing, so dropping the guard would select orders without one
        var sql = Transform(_context.Orders, "eq(customer.contact.label,none)").ToQueryString();

        Where(sql).Should().Contain("CASE", sql);
    }

    [Fact]
    public void Filter_MixingPlainAndComputedMembers_SimplifiesOnlyThePlainOne()
    {
        var sql = Transform(_context.Orders, "and(eq(customer.contact.email,bob@x.io),eq(customer.contact.label,none))").ToQueryString();

        var where = Where(sql);
        where.Should().Contain("\"p\".\"Email\" = @", sql);
        where.Should().NotContain("THEN \"p\".\"Email\"", sql);
        where.Should().Contain("CASE", sql);
    }

    [Fact]
    public void Filter_ReusesTheProjectionJoins()
    {
        var unfiltered = Transform(_context.Orders, null).ToQueryString();
        var filtered = Transform(_context.Orders, "eq(customer.contact.email,ann@x.io)").ToQueryString();

        CountOf(filtered, "JOIN").Should().Be(CountOf(unfiltered, "JOIN"), filtered);
    }

    [Fact]
    public void Filter_KeepsNullReferencesInTheProjection()
    {
        var orders = Transform(_context.Orders, "eq(customer.contact.email,null())").ToList();

        orders.Select(o => o.Id).Should().Equal(3, 4);
        orders[0].Customer.Should().NotBeNull();
        orders[0].Customer!.Contact.Should().BeNull();
        orders[1].Customer.Should().BeNull();
    }

    private static IQueryable<OrderView> Transform(IQueryable<Order> source, string? filter,
        NavigationStrategy navigation = NavigationStrategy.Default, FilterPlacement placement = FilterPlacement.BeforeProjection)
    {
        var response = Rql.Transform(source, new RqlRequest { Filter = filter, Order = "id" }, options =>
        {
            options.Settings.Filter.Navigation = navigation;
            options.Settings.Filter.Placement = placement;
            options.Settings.Mapping.Navigation = navigation;
        });

        response.IsSuccess.Should().BeTrue(string.Join("; ", response.Errors.Select(e => e.Message)));
        return response.Query;
    }

    private static string Where(string sql)
    {
        var start = sql.IndexOf("WHERE", StringComparison.Ordinal);
        start.Should().BeGreaterThan(0, sql);

        var end = sql.LastIndexOf("ORDER BY", StringComparison.Ordinal);
        return end < start ? sql[start..] : sql[start..end];
    }

    private static int CountOf(string text, string value)
        => (text.Length - text.Replace(value, string.Empty, StringComparison.Ordinal).Length) / value.Length;

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
