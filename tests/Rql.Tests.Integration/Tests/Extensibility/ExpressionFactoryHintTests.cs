using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Mpt.Rql;
using Mpt.Rql.Services.Mapping;
using Rql.Tests.Integration.Core;
using System.Linq.Expressions;
using Xunit;

namespace Rql.Tests.Integration.Tests.Extensibility;

public class ExpressionFactoryHintTests
{
    [Fact]
    public void TakeFirst_ShouldMapCollectionToFirstElement()
    {
        // Arrange
        var rql = RqlFactory.Make<Product, ProductFirstOrderView>(
            services =>
            {
                services.AddTransient<FirstOrderFactory>();
            },
            config => config.ScanForMappers(typeof(ExpressionFactoryHintTests).Assembly));

        var testData = ProductRepository.Query();

        // Act
        var result = rql.Transform(testData, new RqlRequest { Select = "Id,FirstOrder" });

        // Assert
        result.IsSuccess.Should().BeTrue();
        var mapped = result.Query.ToList();

        mapped.Should().HaveCountGreaterThan(0);

        // Product 1 has orders: [{ Id=1, ClientName="Michael" }, { Id=2, ClientName="Tony" }, { Id=3, ClientName="Isabel" }]
        var product1 = mapped.First(m => m.Id == 1);
        product1.FirstOrder.Should().NotBeNull();
        product1.FirstOrder!.Id.Should().Be(1);
        product1.FirstOrder.ClientName.Should().Be("Michael");
    }

    [Fact]
    public void TakeFirst_WhenCollectionIsEmpty_ShouldReturnNull()
    {
        // Arrange
        var rql = RqlFactory.Make<Product, ProductFirstOrderView>(
            services =>
            {
                services.AddTransient<FirstOrderFactory>();
            },
            config => config.ScanForMappers(typeof(ExpressionFactoryHintTests).Assembly));

        var testData = ProductRepository.Query();

        // Act
        var result = rql.Transform(testData, new RqlRequest { Select = "Id,FirstOrder" });

        // Assert
        result.IsSuccess.Should().BeTrue();
        var mapped = result.Query.ToList();

        // Product 2 has empty orders
        var product2 = mapped.First(m => m.Id == 2);
        product2.FirstOrder.Should().BeNull();
    }

    [Fact]
    public void TakeFirst_ShouldMapInnerPropertiesCorrectly()
    {
        // Arrange
        var rql = RqlFactory.Make<Product, ProductFirstOrderView>(
            services =>
            {
                services.AddTransient<FirstOrderFactory>();
            },
            config => config.ScanForMappers(typeof(ExpressionFactoryHintTests).Assembly));

        var testData = ProductRepository.Query();

        // Act
        var result = rql.Transform(testData, new RqlRequest { Select = "Id,FirstOrder" });

        // Assert
        result.IsSuccess.Should().BeTrue();
        var mapped = result.Query.ToList();

        // Product 7 has one order: [{ Id=1, ClientName="Michael" }]
        var product7 = mapped.First(m => m.Id == 7);
        product7.FirstOrder.Should().NotBeNull();
        product7.FirstOrder!.Id.Should().Be(1);
        product7.FirstOrder.ClientName.Should().Be("Michael");
    }

    [Fact]
    public void TakeFirst_ShouldCoexistWithRegularMappings()
    {
        // Arrange
        var rql = RqlFactory.Make<Product, ProductMixedView>(
            services =>
            {
                services.AddTransient<FirstOrderForMixedFactory>();
            },
            config => config.ScanForMappers(typeof(ExpressionFactoryHintTests).Assembly));

        var testData = ProductRepository.Query();

        // Act
        var result = rql.Transform(testData, new RqlRequest { Select = "Id,Name,FirstOrder" });

        // Assert
        result.IsSuccess.Should().BeTrue();
        var mapped = result.Query.ToList();

        var product1 = mapped.First(m => m.Id == 1);
        product1.Name.Should().Be("Jewelry Widget");
        product1.FirstOrder.Should().NotBeNull();
        product1.FirstOrder!.ClientName.Should().Be("Michael");
    }

    [Fact]
    public void DefaultHint_ShouldNotAffectExistingCollectionBehavior()
    {
        // Arrange - Factory without hint override (defaults to None)
        var rql = RqlFactory.Make<Product, ProductOrderListView>(
            services =>
            {
                services.AddTransient<OrderListFactory>();
            },
            config => config.ScanForMappers(typeof(ExpressionFactoryHintTests).Assembly));

        var testData = ProductRepository.Query();

        // Act
        var result = rql.Transform(testData, new RqlRequest { Select = "Id,Orders" });

        // Assert
        result.IsSuccess.Should().BeTrue();
        var mapped = result.Query.ToList();

        var product1 = mapped.First(m => m.Id == 1);
        product1.Orders.Should().HaveCount(3);
    }

    [Fact]
    public void TakeFirst_WithPrimitiveCollection_ShouldReturnFirstElement()
    {
        // Arrange
        var rql = RqlFactory.Make<Product, ProductFirstOrderIdView>(
            services =>
            {
                services.AddTransient<FirstOrderIdFactory>();
            },
            config => config.ScanForMappers(typeof(ExpressionFactoryHintTests).Assembly));

        var testData = ProductRepository.Query();

        // Act
        var result = rql.Transform(testData, new RqlRequest { Select = "Id,FirstOrderId" });

        // Assert
        result.IsSuccess.Should().BeTrue();
        var mapped = result.Query.ToList();

        // Product 1 has OrdersIds = [1, 2, 3]
        var product1 = mapped.First(m => m.Id == 1);
        product1.FirstOrderId.Should().Be(1);
    }

    [Fact]
    public void TakeFirst_WithPrimitiveCollection_WhenEmpty_ShouldReturnDefault()
    {
        // Arrange
        var rql = RqlFactory.Make<Product, ProductFirstOrderIdView>(
            services =>
            {
                services.AddTransient<FirstOrderIdFactory>();
            },
            config => config.ScanForMappers(typeof(ExpressionFactoryHintTests).Assembly));

        var testData = ProductRepository.Query();

        // Act
        var result = rql.Transform(testData, new RqlRequest { Select = "Id,FirstOrderId" });

        // Assert
        result.IsSuccess.Should().BeTrue();
        var mapped = result.Query.ToList();

        // Product 2 has OrdersIds = []
        var product2 = mapped.First(m => m.Id == 2);
        product2.FirstOrderId.Should().Be(0);
    }

    [Fact]
    public void TakeFirst_WithNonCollectionExpression_ShouldThrowMappingException()
    {
        // Arrange
        var rql = RqlFactory.Make<Product, ProductBadFactoryView>(
            services =>
            {
                services.AddTransient<NonCollectionTakeFirstFactory>();
            },
            config => config.ScanForMappers(typeof(ExpressionFactoryHintTests).Assembly));

        var testData = ProductRepository.Query();

        // Act & Assert
        var exception = Assert.Throws<RqlMappingException>(() =>
            rql.Transform(testData, new RqlRequest { Select = "Id,BadField" }));

        exception.Message.Should().Contain("TakeFirst");
        exception.Message.Should().Contain("BadField");
        exception.Message.Should().Contain("collection");
    }
}

// View models
internal class ProductFirstOrderView
{
    [RqlProperty(IsCore = true)]
    public int Id { get; set; }

    [RqlProperty(IsCore = true)]
    public OrderView? FirstOrder { get; set; }
}

internal class ProductMixedView
{
    [RqlProperty(IsCore = true)]
    public int Id { get; set; }

    [RqlProperty(IsCore = true)]
    public string Name { get; set; } = null!;

    [RqlProperty(IsCore = true)]
    public OrderView? FirstOrder { get; set; }
}

internal class ProductOrderListView
{
    [RqlProperty(IsCore = true)]
    public int Id { get; set; }

    [RqlProperty(IsCore = true)]
    public List<OrderView> Orders { get; set; } = null!;
}

internal class OrderView
{
    [RqlProperty(IsCore = true)]
    public int Id { get; set; }

    [RqlProperty(IsCore = true)]
    public string ClientName { get; set; } = null!;
}

// Mappers
internal class ProductToFirstOrderViewMapper : IRqlMapper<Product, ProductFirstOrderView>
{
    public void MapEntity(IRqlMapperContext<Product, ProductFirstOrderView> context)
    {
        context.MapStatic(v => v.Id, s => s.Id);
        context.MapWithFactory<FirstOrderFactory>(v => v.FirstOrder);
    }
}

internal class ProductToMixedViewMapper : IRqlMapper<Product, ProductMixedView>
{
    public void MapEntity(IRqlMapperContext<Product, ProductMixedView> context)
    {
        context.MapStatic(v => v.Id, s => s.Id);
        context.MapStatic(v => v.Name, s => s.Name);
        context.MapWithFactory<FirstOrderForMixedFactory>(v => v.FirstOrder);
    }
}

internal class ProductToOrderListViewMapper : IRqlMapper<Product, ProductOrderListView>
{
    public void MapEntity(IRqlMapperContext<Product, ProductOrderListView> context)
    {
        context.MapStatic(v => v.Id, s => s.Id);
        context.MapWithFactory<OrderListFactory>(v => v.Orders);
    }
}

// Factories
internal class FirstOrderFactory : IRqlMappingExpressionFactory<Product>
{
    public ExpressionFactoryHint Hint => ExpressionFactoryHint.TakeFirst;

    public Expression<Func<Product, object?>> GetStorageExpression()
        => p => p.Orders;
}

internal class FirstOrderForMixedFactory : IRqlMappingExpressionFactory<Product>
{
    public ExpressionFactoryHint Hint => ExpressionFactoryHint.TakeFirst;

    public Expression<Func<Product, object?>> GetStorageExpression()
        => p => p.Orders;
}

internal class OrderListFactory : IRqlMappingExpressionFactory<Product>
{
    // No Hint override — defaults to None

    public Expression<Func<Product, object?>> GetStorageExpression()
        => p => p.Orders;
}

// Primitive collection TakeFirst
internal class ProductFirstOrderIdView
{
    [RqlProperty(IsCore = true)]
    public int Id { get; set; }

    [RqlProperty(IsCore = true)]
    public int FirstOrderId { get; set; }
}

internal class ProductToFirstOrderIdViewMapper : IRqlMapper<Product, ProductFirstOrderIdView>
{
    public void MapEntity(IRqlMapperContext<Product, ProductFirstOrderIdView> context)
    {
        context.MapStatic(v => v.Id, s => s.Id);
        context.MapWithFactory<FirstOrderIdFactory>(v => v.FirstOrderId);
    }
}

internal class FirstOrderIdFactory : IRqlMappingExpressionFactory<Product>
{
    public ExpressionFactoryHint Hint => ExpressionFactoryHint.TakeFirst;

    public Expression<Func<Product, object?>> GetStorageExpression()
        => p => p.OrdersIds;
}

// Non-collection TakeFirst (should fail)
internal class ProductBadFactoryView
{
    [RqlProperty(IsCore = true)]
    public int Id { get; set; }

    [RqlProperty(IsCore = true)]
    public string BadField { get; set; } = null!;
}

internal class ProductToBadFactoryViewMapper : IRqlMapper<Product, ProductBadFactoryView>
{
    public void MapEntity(IRqlMapperContext<Product, ProductBadFactoryView> context)
    {
        context.MapStatic(v => v.Id, s => s.Id);
        context.MapWithFactory<NonCollectionTakeFirstFactory>(v => v.BadField);
    }
}

internal class NonCollectionTakeFirstFactory : IRqlMappingExpressionFactory<Product>
{
    public ExpressionFactoryHint Hint => ExpressionFactoryHint.TakeFirst;

    public Expression<Func<Product, object?>> GetStorageExpression()
        => p => p.Name; // Not a collection — should throw
}
