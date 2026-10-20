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
/// Executes the same filters with view and storage binding against a real relational provider (SQLite in memory)
/// and checks they select the same rows, including rows whose nested references are missing.
/// </summary>
public sealed class SqliteParityTests : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");
    private readonly QuotesDbContext _context;

    private readonly IRqlQueryable<Quote, QuoteView> _rql = new ServiceCollection()
        .AddRql(rql =>
        {
            rql.Settings.Select.Implicit = RqlSelectModes.Core | RqlSelectModes.Primitive | RqlSelectModes.Reference;
            rql.Settings.Select.Explicit = RqlSelectModes.All;
            rql.Settings.Select.MaxDepth = 10;
        })
        .BuildServiceProvider()
        .GetRequiredService<IRqlQueryable<Quote, QuoteView>>();

    public SqliteParityTests()
    {
        _connection.Open();
        _context = new QuotesDbContext(new DbContextOptionsBuilder<QuotesDbContext>().UseSqlite(_connection).Options);
        _context.Database.EnsureCreated();

        _context.Quotes.AddRange(
            new Quote { Id = 1, Number = "Q1", SupportCase = new SupportCase { Id = 10, Status = "Open", Reporter = new Contact { Id = 100, Email = "ann@x.io", Phone = "1" } }, Lines = [new QuoteLine { Id = 1, Sku = "ABC" }] },
            new Quote { Id = 2, Number = "Q2", SupportCase = new SupportCase { Id = 20, Status = "Closed", Reporter = new Contact { Id = 200, Email = "bob@x.io", Phone = null } } },
            new Quote { Id = 3, Number = "Q3", SupportCase = new SupportCase { Id = 30, Status = "Open", Reporter = null }, Lines = [new QuoteLine { Id = 2, Sku = "XYZ" }] },
            new Quote { Id = 4, Number = "Q4", SupportCase = null });
        _context.SaveChanges();
        _context.ChangeTracker.Clear();
    }

    public static TheoryData<string, int[]> Filters => new()
    {
        { "eq(supportCase.reporter.email,ann@x.io)", [1] },
        { "ne(supportCase.reporter.email,ann@x.io)", [2, 3, 4] },
        { "eq(supportCase.reporter.email,null())", [3, 4] },
        { "ne(supportCase.reporter.email,null())", [1, 2] },
        { "eq(supportCase.reporter.phone,null())", [2, 3, 4] },
        { "like(supportCase.reporter.email,*@x.io)", [1, 2] },
        { "like(supportCase.reporter.email,bob*)", [2] },
        { "in(supportCase.reporter.email,(ann@x.io,bob@x.io))", [1, 2] },
        { "out(supportCase.reporter.email,(ann@x.io,bob@x.io))", [3, 4] },
        { "gt(supportCase.id,15)", [2, 3] },
        { "in(supportCase.id,(10,20))", [1, 2] },
        { "out(supportCase.id,(10,20))", [3, 4] },
        { "le(supportCase.id,20)", [1, 2] },
        { "eq(supportCase.status,Open)", [1, 3] },
        { "and(eq(supportCase.status,Open),ne(supportCase.reporter.email,null()))", [1] },
        { "or(eq(supportCase.reporter.email,bob@x.io),eq(number,Q4))", [2, 4] },
        { "not(eq(supportCase.reporter.email,ann@x.io))", [2, 3, 4] },
        { "any(lines,eq(sku,ABC))", [1] },
        { "any(lines)", [1, 3] },
        { "eq(number,Q2)", [2] },
    };

    [Theory]
    [MemberData(nameof(Filters))]
    public void StorageBinding_SelectsTheSameRowsAsViewBinding(string filter, int[] expected)
    {
        var view = Ids(filter, FilterBinding.View, NavigationStrategy.Default);
        var storage = Ids(filter, FilterBinding.Storage, NavigationStrategy.Default);

        storage.Should().Equal(expected);
        view.Should().Equal(expected);
    }

    [Theory]
    [MemberData(nameof(Filters))]
    public void StorageBinding_WithSafeNavigation_SelectsTheRowsTheViewBindingSelectsInMemory(string filter, int[] expectedWithDefaultNavigation)
    {
        // Safe navigation has its own null semantics (ne() on a null path is false), so the reference is the view
        // binding evaluated under LINQ-to-Objects: on the database it compares a projected object with null, which
        // EF cannot translate, while the storage binding reduces that comparison to the guard itself.
        var loaded = _context.Quotes.Include(q => q.SupportCase!).ThenInclude(c => c.Reporter).Include(q => q.Lines).AsNoTracking().ToList().AsQueryable();
        var expected = Ids(loaded, filter, FilterBinding.View, NavigationStrategy.Safe);

        Ids(_context.Quotes, filter, FilterBinding.Storage, NavigationStrategy.Safe).Should().Equal(expected);
        _ = expectedWithDefaultNavigation;
    }

    private List<int> Ids(string filter, FilterBinding binding, NavigationStrategy navigation)
        => Ids(_context.Quotes, filter, binding, navigation);

    private List<int> Ids(IQueryable<Quote> source, string filter, FilterBinding binding, NavigationStrategy navigation)
    {
        var response = _rql.Transform(source, new RqlRequest { Filter = filter, Order = "id" }, options =>
        {
            options.Settings.Filter.Binding = binding;
            options.Settings.Filter.Navigation = navigation;
            options.Settings.Mapping.Navigation = navigation;
            options.Settings.Ordering.Navigation = navigation;
        });

        response.IsSuccess.Should().BeTrue(string.Join("; ", response.Errors.Select(e => e.Message)));
        return response.Query.Select(q => q.Id).ToList();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}
