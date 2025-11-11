using FluentAssertions;
using Mpt.Rql.Client.Builder.Operators;
using Mpt.Rql.Client.Core;
using Mpt.Rql.Core.Metadata;
using Rql.Tests.Unit.Client.Samples;
using Xunit;

namespace Rql.Tests.Unit.Client.Builder;

public class MultiComparisionOperatorTests
{
    private readonly PropertyVisitor _propertyVisitor;

    public MultiComparisionOperatorTests()
    {
        _propertyVisitor = new PropertyVisitor(new PropertyNameProvider());
    }

    [Fact]
    public void ToQueryOperator_WhenMultipleValues_ThenMultipleValuesAreGenerated()
    {
        // Arrange
        var op = new In<User, int>(x => x.Id, new List<int> { 1, 2, 3 });

        // Act
        var (property, value) = op.ToQueryOperator(_propertyVisitor);

        // Assert
        value.Should().Be("1,2,3");
    }

    [Fact]
    public void ToQueryOperator_WhenSingleValue_ThenValueIsGenerated()
    {
        // Arrange 
        var op = new In<User, int>(x => x.Id, new List<int> { 1 });

        // Act
        var (property, value) = op.ToQueryOperator(_propertyVisitor);

        // Assert
        value.Should().Be("1");
    }

    [Fact]
    public void ToQueryOperator_WhenEmpty_ThenValueIsGenerated()
    {
        // Arrange
        var op = new In<User, int>(x => x.Id, new List<int>());

        // Act
        var (property, value) = op.ToQueryOperator(_propertyVisitor);

        // Assert
        value.Should().Be(string.Empty);
    }
}