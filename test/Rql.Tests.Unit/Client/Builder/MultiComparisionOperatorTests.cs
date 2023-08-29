using FluentAssertions;
using Rql.Tests.Unit.Client.Models;
using SoftwareOne.Rql.Client;
using SoftwareOne.Rql.Linq.Client.Dsl;
using Xunit;

namespace Rql.Tests.Unit.Client.Builder;

public class MultiComparisionOperatorTests
{
    [Fact]
    public void ToQueryOperator_WhenMultipleValues_ThenMultipleValuesAreGenerated()
    {
        // Arrange
        var op = new In<User, int>(x => x.Id, new List<int> { 1, 2, 3 });

        // Act
        var (property, value) = op.ToQueryOperator();

        // Assert
        value.Should().Be("1,2,3");
    }

    [Fact]
    public void ToQueryOperator_WhenSingleValue_ThenValueIsGenerated()
    {
        // Arrange 
        var op = new In<User, int>(x => x.Id, new List<int> { 1 });

        // Act
        var (property, value) = op.ToQueryOperator();

        // Assert
        value.Should().Be("1");
    }

    [Fact]
    public void ToQueryOperator_WhenEmpty_ThenValueIsGenerated()
    {
        // Arrange
        var op = new In<User, int>(x => x.Id, new List<int>());

        // Act
        var (property, value) = op.ToQueryOperator();

        // Assert
        value.Should().Be(string.Empty);
    }
}