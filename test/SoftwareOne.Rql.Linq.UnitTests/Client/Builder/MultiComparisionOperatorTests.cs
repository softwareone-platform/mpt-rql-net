using FluentAssertions;
using SoftwareOne.Rql.Linq.Client.Builder.Operators;
using SoftwareOne.Rql.Linq.Client.Core;
using SoftwareOne.Rql.Linq.Core.Metadata;
using SoftwareOne.Rql.Linq.UnitTests.Client.Samples;
using Xunit;

namespace SoftwareOne.Rql.Linq.UnitTests.Client.Builder;

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