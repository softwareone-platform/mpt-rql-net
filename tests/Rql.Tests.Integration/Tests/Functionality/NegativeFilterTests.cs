using Mpt.Rql;
using Mpt.Rql.Abstractions.Exception;
using Mpt.Rql.Abstractions.Result;
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

    [Theory]
    [InlineData("someBareWord")]
    [InlineData("name")]
    [InlineData("123")]
    [InlineData("*")]
    [InlineData("reference.name")]
    [InlineData("(someBareWord)")]
    [InlineData("and(someBareWord)")]
    [InlineData("not(someBareWord)")]
    [InlineData("or(name,eq(price,1))")]
    [InlineData("and(eq(price,1),bareWord)")]
    [InlineData("any(Orders,bareWord)")]
    public void BareValue_UsedAsExpression_ReturnsValidationError(string query)
    {
        // Arrange
        var testData = ProductRepository.Query();

        // Act
        var result = _rql.Transform(testData, new RqlRequest { Filter = query });

        // Assert
        Assert.False(result.IsSuccess);
        Assert.All(result.Errors, e => Assert.Equal(ErrorType.Validation, e.Type));
        Assert.DoesNotContain(result.Errors, e => e.Message.Contains("RQL package maintainer"));
    }

    [Theory]
    [InlineData("()")]
    [InlineData("and()")]
    [InlineData("and(,)")]
    public void EmptyExpressionGroup_ReturnsValidationError(string query)
    {
        // Arrange
        var testData = ProductRepository.Query();

        // Act
        var result = _rql.Transform(testData, new RqlRequest { Filter = query });

        // Assert
        Assert.False(result.IsSuccess);
        Assert.All(result.Errors, e => Assert.Equal(ErrorType.Validation, e.Type));
        Assert.DoesNotContain(result.Errors, e => e.Message.Contains("RQL package maintainer"));
    }

    [Theory]
    [InlineData("any()")]
    [InlineData("all()")]
    public void Collection_WithoutArguments_ThrowsRqlCollectionParserException(string query)
    {
        // Arrange
        var testData = ProductRepository.Query();

        // Act and Assert
        var exception = Assert.Throws<RqlCollectionParserException>(() =>
            _rql.Transform(testData, new RqlRequest { Filter = query }));

        Assert.Equal("Collection expression must have at least 1 argument", exception.Message);
    }
}