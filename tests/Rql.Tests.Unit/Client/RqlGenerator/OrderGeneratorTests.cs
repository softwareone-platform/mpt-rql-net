using FluentAssertions;
using Mpt.Rql.Client.Builder.Order;
using Mpt.Rql.Client.Core;
using Mpt.Rql.Client.Generator;
using Mpt.Rql.Core.Metadata;
using Rql.Tests.Unit.Client.Samples;
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
        result.Should().Be("-firstName,homeAddress");
    }
}