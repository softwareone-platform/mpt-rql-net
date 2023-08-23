using FluentAssertions;
using Rql.Tests.Unit.Client.Models;
using SoftwareOne.Rql.Client.Builder.Order;
using SoftwareOne.Rql.Client.RqlGenerator;
using Xunit;

namespace Rql.Tests.Unit.Client.RqlGenerator;

public class OrderGeneratorTests
{
    [Fact]
    public void WhenNull_StringEmpty()
    {
        // Arrange
        var def = new OrderContext<User>().GetDefinition();

        // Act 
        var result = new OrderGenerator().Generate(def);

        // Assert
        result.Should().BeNullOrEmpty(result);
    }
    
    [Fact]
    public void WhenOrdered_ThenGenerated()
    {
        // Arrange
        var def = new OrderContext<User>()
            .OrderBy(x => x.FirstName, OrderDirection.Descending)
            .OrderBy(x => x.HomeAddress, OrderDirection.Ascending).GetDefinition();

        // Act 
        var result = new OrderGenerator().Generate(def);

        // Assert
        result.Should().Be("order=(-FirstName,HomeAddress)");
    }
}