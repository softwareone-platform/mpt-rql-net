using Mpt.Rql;
using Rql.Tests.Integration.Core;
using Xunit;

namespace Rql.Tests.Integration.Tests.Functionality;

public class NegativeFilterTests
{
    private readonly IRqlQueryable<Product, Product> _rql;

    public NegativeFilterTests()
    {
        _rql = RqlFactory.Make<Product>(services => { }, rql =>
        {
            rql.Settings.Select.Implicit = RqlSelectModes.Core | RqlSelectModes.Primitive | RqlSelectModes.Reference;
            rql.Settings.Select.Explicit = RqlSelectModes.All;
            rql.Settings.Select.MaxDepth = 10;
        });
    }

    [Theory]
    [InlineData("any(Orders,self(abc)=1)")]
    [InlineData("any(Orders,eq(self(abc),1))")]
    public void Any_SaleDetailIds_Equals(string query)
    {
        // Arrange
        var testData = ProductRepository.Query();

        // Act
        var result = _rql.Transform(testData, new RqlRequest { Filter = query });

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Invalid property path.", result.Errors.First().Message);
    }
}