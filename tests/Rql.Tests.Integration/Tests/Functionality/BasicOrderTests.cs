using Mpt.Rql;
using Rql.Tests.Integration.Core;
using Xunit;

namespace Rql.Tests.Integration.Tests.Functionality;

public class BasicOrderTests
{
    private readonly IRqlQueryable<Product, Product> _rql;

    public BasicOrderTests()
    {
        _rql = RqlFactory.Make<Product>(services => { }, rql =>
        {
            rql.Settings.Select.Implicit = RqlSelectModes.Core | RqlSelectModes.Primitive | RqlSelectModes.Reference;
            rql.Settings.Select.Explicit = RqlSelectModes.All;
            rql.Settings.Select.MaxDepth = 10;
        });
    }

    [Theory]
    [InlineData("+category,+id,-name")]
    [InlineData("category,id,-name")]
    [InlineData("-category,-name", false)]
    public void Ordering_CategoryAsc_NameDesc(string order, bool isHappyFlow = true)
    {
        // Arrange
        var testData = ProductRepository.Query();

        // Act
        var result = _rql.Transform(testData, new RqlRequest { Order = order });

        // Assert
        Assert.True(result.IsSuccess);
        var products = result.Query.ToList();
        
        Assert.Equal(8, products.Count);
        
        if (isHappyFlow)
        {
            // Verify correct ordering by checking sequence
            Assert.Equal(6, products[0].Id); // Empty category first
            Assert.Equal(2, products[1].Id); // Activity category second
            Assert.Equal(3, products[2].Id); // Activity category third
        }
    }

    [Fact]
    public void Ordering_By_IsIgnored_ThrowsException()
    {
        // Arrange
        var testData = ProductRepository.Query();

        // Act
        var result = _rql.Transform(testData, new RqlRequest { Order = "ignored" });

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Invalid property path.", result.Errors.First().Message);
    }
}