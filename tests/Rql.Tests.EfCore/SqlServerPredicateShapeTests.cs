using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mpt.Rql;
using Mpt.Rql.Abstractions.Configuration;
using Mpt.Rql.Abstractions.Configuration.Filter;
using Xunit;

namespace Rql.Tests.EfCore;

/// <summary>
/// The SQL the SQL Server provider generates for filters through nullable nested references, without a database
/// (<c>ToQueryString</c> only). With view binding the compared column sits inside a <c>CASE WHEN</c>, which SQL
/// Server cannot seek on; with storage binding it is a plain column predicate.
/// </summary>
public class SqlServerPredicateShapeTests
{
    private readonly IRqlQueryable<Quote, QuoteView> _rql = new ServiceCollection()
        .AddRql(rql =>
        {
            rql.Settings.Select.Implicit = RqlSelectModes.Core | RqlSelectModes.Primitive | RqlSelectModes.Reference;
            rql.Settings.Select.Explicit = RqlSelectModes.All;
            rql.Settings.Select.MaxDepth = 10;
        })
        .BuildServiceProvider()
        .GetRequiredService<IRqlQueryable<Quote, QuoteView>>();

    private readonly QuotesDbContext _context = new(new DbContextOptionsBuilder<QuotesDbContext>()
        .UseSqlServer("Server=(local);Database=rql;Trusted_Connection=True;TrustServerCertificate=True")
        .Options);

    [Theory]
    [InlineData("eq(supportCase.reporter.email,a@b.c)", "[Email] = @")]
    [InlineData("eq(supportCase.reporter.id,7)", "[Id] = @")]
    [InlineData("ne(supportCase.reporter.email,a@b.c)", "[Email] <> @")]
    [InlineData("eq(supportCase.reporter.email,null())", "[Email] IS NULL")]
    [InlineData("ne(supportCase.reporter.phone,null())", "[Phone] IS NOT NULL")]
    [InlineData("like(supportCase.reporter.email,a*)", "[Email] LIKE @")]
    [InlineData("in(supportCase.reporter.email,(a@b.c,d@e.f))", "[Email] IN (")]
    [InlineData("gt(supportCase.id,7)", "[Id] > @")]
    [InlineData("in(supportCase.id,(7,8))", "[Id] IN (")]
    [InlineData("eq(supportCase.status,Open)", "[Status] = @")]
    [InlineData("any(lines,eq(sku,ABC))", "[Sku] = @")]
    public void StorageBinding_FiltersOnPlainColumns(string filter, string expectedPredicate)
    {
        var sql = Sql(filter, FilterBinding.Storage, NavigationStrategy.Default);

        var where = WhereClause(sql);
        where.Should().NotContain("CASE", sql);
        where.Should().Contain(expectedPredicate, sql);
    }

    [Theory]
    [InlineData("eq(supportCase.reporter.email,a@b.c)")]
    [InlineData("ne(supportCase.reporter.email,a@b.c)")]
    [InlineData("like(supportCase.reporter.email,a*)")]
    public void ViewBinding_FiltersThroughACaseExpression(string filter)
    {
        // documents the shape the storage binding exists to avoid
        var sql = Sql(filter, FilterBinding.View, NavigationStrategy.Default);

        WhereClause(sql).Should().Contain("CASE", sql);
    }

    [Theory]
    [InlineData("eq(supportCase.reporter.email,a@b.c)", "[Email] = @")]
    [InlineData("ne(supportCase.reporter.email,a@b.c)", "[Email] <> @")]
    [InlineData("like(supportCase.reporter.email,a*)", "[Email] LIKE @")]
    [InlineData("gt(supportCase.id,7)", "[Id] > @")]
    public void StorageBinding_WithSafeNavigation_TranslatesToPlainColumns(string filter, string expectedPredicate)
    {
        var sql = Sql(filter, FilterBinding.Storage, NavigationStrategy.Safe);

        var where = WhereClause(sql);
        where.Should().NotContain("CASE", sql);
        where.Should().Contain(expectedPredicate, sql);
    }

    [Fact]
    public void StorageBinding_AppliesTheGuardsAsNullChecksOnTheJoinedKeys()
    {
        var sql = Sql("eq(supportCase.reporter.email,a@b.c)", FilterBinding.Storage, NavigationStrategy.Default);

        WhereClause(sql).Should().Contain("[Id] IS NOT NULL AND", sql);
    }

    private string Sql(string filter, FilterBinding binding, NavigationStrategy navigation)
    {
        var response = _rql.Transform(_context.Quotes, new RqlRequest { Filter = filter }, options =>
        {
            options.Settings.Filter.Binding = binding;
            options.Settings.Filter.Navigation = navigation;
            options.Settings.Mapping.Navigation = navigation;
        });

        response.IsSuccess.Should().BeTrue(string.Join("; ", response.Errors.Select(e => e.Message)));
        return response.Query.ToQueryString();
    }

    private static string WhereClause(string sql)
    {
        var start = sql.IndexOf("WHERE", StringComparison.Ordinal);
        start.Should().BeGreaterThan(0, sql);

        var end = sql.IndexOf("ORDER BY", start, StringComparison.Ordinal);
        return end < 0 ? sql[start..] : sql[start..end];
    }
}
