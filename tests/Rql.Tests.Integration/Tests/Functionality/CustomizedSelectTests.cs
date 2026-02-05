using Rql.Tests.Integration.Core;
using Mpt.Rql;
using Xunit;

namespace Rql.Tests.Integration.Tests.Functionality;

public class CustomizedSelectTests
{
    private readonly IRqlQueryable<ShapedProduct, ShapedProduct> _rql;

    public CustomizedSelectTests()
    {
        _rql = RqlFactory.Make<ShapedProduct>(services => { }, rql =>
        {
            rql.Settings.Select.Implicit = RqlSelectModes.Core | RqlSelectModes.Primitive | RqlSelectModes.Reference;
            rql.Settings.Select.Explicit = RqlSelectModes.All;
            rql.Settings.Select.MaxDepth = 99;
        });
    }

    [Fact]
    public void Shape_NoChange()
    {
        // Arrange
        var testData = ShapedProductRepository.Query();

        // Act
        var result = _rql.Transform(testData, new RqlRequest { Select = string.Empty });

        // Assert
        Assert.True(result.IsSuccess);
        var shape = result.Query.First();
        Assert.Null(shape.HiddenCollection);
        Assert.Null(shape.Ignored);
    }

    [Fact]
    public void Shape_HiddenCollection_Included()
    {
        // Arrange
        var testData = ShapedProductRepository.Query();

        // Act
        var result = _rql.Transform(testData, new RqlRequest { Select = "HiddenCollection,-Ignored" });

        // Assert
        Assert.True(result.IsSuccess);
        var shape = result.Query.First();
        Assert.Null(shape.Ignored);
        Assert.NotNull(shape.HiddenCollection);
    }

    [Fact]
    public void Shape_Collection_Excluded()
    {
        // Arrange
        var testData = ShapedProductRepository.Query();

        // Act
        var result = _rql.Transform(testData, new RqlRequest { Select = "-Collection" });

        // Assert
        Assert.True(result.IsSuccess);
        var shape = result.Query.First();
        Assert.Null(shape.Collection);
        Assert.Null(shape.HiddenCollection);
        Assert.Null(shape.Ignored);
    }

    [Fact]
    public void Shape_Reference_Excluded()
    {
        // Arrange
        var testData = ShapedProductRepository.Query();

        // Act
        var result = _rql.Transform(testData, new RqlRequest { Select = "-Reference" });

        // Assert
        Assert.True(result.IsSuccess);
        var shape = result.Query.First();
        Assert.Null(shape.Reference);
        Assert.Null(shape.HiddenCollection);
        Assert.Null(shape.Ignored);
    }
}