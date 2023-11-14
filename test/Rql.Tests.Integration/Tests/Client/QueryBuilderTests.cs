using FluentAssertions;
using Rql.Tests.Integration.Core;
using SoftwareOne.Rql.Client;
using Xunit;

namespace Rql.Tests.Integration.Tests.Client;

public class QueryBuilderTests
{
    [Fact]
    public void GetQueryBuilder_BuildQueryForProduct_ReturnsProperRequest()
    {
        var provider = RqlFactory.MakeProvider();
        var queryBuilder = (IRqlRequestBuilder<Product>)provider.GetService(typeof(IRqlRequestBuilder<Product>))!;

        Assert.NotNull(queryBuilder);

        var rql = queryBuilder
            .Where(e => e.Eq(f => f.Category, "a"))
            .Build();

        rql.Filter.Should().Be("eq(category,'a')");
    }

    [Fact]
    public void GetQueryBuilderProviderToInstantiateBuilder_ReturnsProperRequest()
    {
        var provider = RqlFactory.MakeProvider();
        var queryBuilder = (IRqlRequestBuilderProvider)provider.GetService(typeof(IRqlRequestBuilderProvider))!;

        Assert.NotNull(queryBuilder);

        var rql = queryBuilder.GetBuilder<Product>()
            .Where(e => e.Eq(f => f.Category, "a"))
            .OrderByDescending(e => e.Category)
            .ThenBy(f => f.Name)
            .Select(e => e.Include(f => f.Category))
            
            .Build();
        rql.Filter.Should().Be("eq(category,'a')");
        rql.Order.Should().Be("-category,name");
    }
}