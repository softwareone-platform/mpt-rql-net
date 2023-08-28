using FluentAssertions;
using Rql.Tests.Unit.Client.Models;
using SoftwareOne.Rql.Client;
using SoftwareOne.Rql.Linq.Client;
using SoftwareOne.Rql.Linq.Client.Builder.Dsl;
using SoftwareOne.Rql.Linq.Client.Builder.Order;
using SoftwareOne.Rql.Linq.Client.Builder.Paging;
using SoftwareOne.Rql.Linq.Client.Builder.Select;
using SoftwareOne.Rql.Linq.Client.RqlGenerator;
using Xunit;

namespace Rql.Tests.Unit.Client.RqlGenerator;

public class QueryGeneratorTests
{
    [Fact]
    public void WhenQueryAndSelectAndPagingAreNotEmpty_ThenAreGeneratedInRql()
    {
        // Arrange
        var query = new QueryBuilder<User>()
            .WithQuery(context => context.Eq(x => x.HomeAddress.Street, "abc"))
            .WithSelect(context => context.Include(x => x.HomeAddress).Exclude(x => x.OfficeAddress)) 
            .WithPaging(100, 10) 
            .Build();

        // Act
        var rql = new QueryGenerator(new QueryParamsGenerator(), new SelectGenerator(), new PagingGenerator(), new OrderGenerator()).Generate(query).ToString();

        // Assert
        rql.Should().Be("eq(HomeAddress.Street, 'abc')&select=HomeAddress,-OfficeAddress&limit=100&offset=10");
    }

    [Fact]
    public void WhenQueryAndSelectAndPagingAreNotEmptyAndShortSyntax_ThenAreGeneratedInRql()
    {
        // Arrange & Act
        var rql = new QueryBuilder<User>(context => context.Eq(x => x.HomeAddress.Street, "abc"))
            .WithSelect(context => context.Include(x => x.HomeAddress).Exclude(x => x.OfficeAddress))
            .WithPaging(100, 10)
            .BuildString();

        // Assert
        rql.Should().Be("eq(HomeAddress.Street, 'abc')&select=HomeAddress,-OfficeAddress&limit=100&offset=10");
    }

    [Fact]
    public void WhenQueryAndSelectAndPagingAreNotEmptyAndLongSyntax_ThenAreGeneratedInRql()
    {
        // Arrange
        var op = new Equal<User, string>(x => x.HomeAddress.Street, "abc");
        var select = new SelectFields(
            new List<ISelect> { new Select<User, Address>(x => x.HomeAddress) },
            new List<ISelect> { new Select<User, Address>(x => x.OfficeAddress) }
        );

        var query = new Query(
            op,
            new CustomPaging(100, 10),
            select
        );

        // Act
        var rql = new QueryGenerator(new QueryParamsGenerator(), new SelectGenerator(), new PagingGenerator(), new OrderGenerator()).Generate(query).ToString();

        // Assert
        rql.Should().Be("eq(HomeAddress.Street, 'abc')&select=HomeAddress,-OfficeAddress&limit=100&offset=10");
    }

    [Fact]
    public void WhenQueryAndSelectAreNotEmptyAndPagingIsNotDefined_ThenAreGeneratedInRql()
    {
        // Arrange
        var query = new QueryBuilder<User>()
            .WithQuery(context => context.Eq(x => x.HomeAddress.Street, "abc"))
            .WithSelect(context => context.Include(x => x.HomeAddress).Exclude(x => x.OfficeAddress))
            .WithOrder(context => context.OrderBy(x => x.FirstName, OrderDirection.Descending).OrderBy(x => x.LastName, OrderDirection.Ascending))
            .Build();

        // Act
        var rql = new QueryGenerator(new QueryParamsGenerator(), new SelectGenerator(), new PagingGenerator(), new OrderGenerator()).Generate(query).ToString();

        // Assert
        rql.Should().Be("eq(HomeAddress.Street, 'abc')&select=HomeAddress,-OfficeAddress&order=(-FirstName,LastName)");
    }

    [Fact]
    public void WhenQueryIsNotEmptyAndSelectAndPagingAreNotDefined_ThenQueryIsGeneratedInRql()
    {
        // Arrange
        var query = new QueryBuilder<User>()
            .WithQuery(context => context.Eq(x => x.HomeAddress.Street, "abc"))
            .Build();

        // Act
        var rql = new QueryGenerator(new QueryParamsGenerator(), new SelectGenerator(), new PagingGenerator(), new OrderGenerator()).Generate(query);

        // Assert
        rql.ToString().Should().Be("eq(HomeAddress.Street, 'abc')");
    }
}