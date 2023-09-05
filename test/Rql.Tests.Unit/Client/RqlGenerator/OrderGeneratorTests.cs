using FluentAssertions;
using Rql.Tests.Unit.Client.Models;
using Rql.Tests.Unit.Client.Samples;
using SoftwareOne.Rql.Client;
using SoftwareOne.Rql.Linq.Client;
using SoftwareOne.Rql.Linq.Client.Order;
using SoftwareOne.Rql.Linq.Core.Metadata;
using Xunit;

namespace Rql.Tests.Unit.Client.RqlGenerator;

public class OrderGeneratorTests
{
    private readonly PropertyVisitor _propertyVisitor;

    public OrderGeneratorTests()
    {
        _propertyVisitor = new PropertyVisitor(new PropertyNameProvider());
    }

    [Fact]
    public void WhenNull_StringEmpty()
    {
        // Arrange
        IOrderDefinitionProvider holder = new OrderContext<User>();

        // Act 
        var result = new OrderGenerator(_propertyVisitor).Generate(holder);

        // Assert
        result.Should().BeNullOrEmpty(result);
    }

    [Fact]
    public void WhenOrdered_ThenGenerated()
    {
        // Arrange
        IOrderDefinitionProvider holder = ((OrderContext<User>)new OrderContext<User>()
            .OrderByDescending(x => x.FirstName)
            .ThenBy(x => x.HomeAddress));

        // Act 
        var result = new OrderGenerator(_propertyVisitor).Generate(holder);

        // Assert
        result.Should().Be("-FirstName,HomeAddress");
    }
}