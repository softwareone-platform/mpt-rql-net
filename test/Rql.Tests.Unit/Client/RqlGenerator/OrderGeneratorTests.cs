using FluentAssertions;
using Rql.Tests.Unit.Client.Models;
using SoftwareOne.Rql.Linq.Client.Builder.Order;
using SoftwareOne.Rql.Linq.Client.Core;
using SoftwareOne.Rql.Linq.Client.Generator;
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
        var holder = new OrderContext<User>();
        holder.AddOrder(o => o.FirstName, OrderDirection.Descending);
        holder.AddOrder(o => o.HomeAddress, OrderDirection.Ascending);

        // Act 
        var result = new OrderGenerator(_propertyVisitor).Generate(holder);

        // Assert
        result.Should().Be("-FirstName,HomeAddress");
    }
}