using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Mpt.Rql;
using Mpt.Rql.Core.Metadata;
using Mpt.Rql.Services.Mapping;
using System.Linq.Expressions;
using Xunit;

namespace Rql.Tests.Unit.Services.Mapping;

public class ExpressionFactoryHintTests
{
    [Fact]
    public void Hint_Default_ShouldBeNone()
    {
        // Arrange
        var factory = new FactoryWithoutHint();

        // Act
        var hint = ((IRqlMappingExpressionFactory)factory).Hint;

        // Assert
        hint.Should().Be(ExpressionFactoryHint.None);
    }

    [Fact]
    public void Hint_WhenOverridden_ShouldReturnTakeFirst()
    {
        // Arrange
        var factory = new TakeFirstFactory();

        // Act
        var hint = ((IRqlMappingExpressionFactory)factory).Hint;

        // Assert
        hint.Should().Be(ExpressionFactoryHint.TakeFirst);
    }

    [Fact]
    public void MapWithFactory_WithTakeFirstHint_ShouldStoreFactoryType()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<TakeFirstFactory>();
        var ctx = MakeContext<Storage, SingleItemView>(services);

        // Act
        ctx.MapWithFactory<TakeFirstFactory>(v => v.FirstOrder);

        // Assert
        var mapping = ctx.Mapping;
        mapping.Should().ContainKey(nameof(SingleItemView.FirstOrder));
        mapping[nameof(SingleItemView.FirstOrder)].FactoryType.Should().Be(typeof(TakeFirstFactory));
        mapping[nameof(SingleItemView.FirstOrder)].IsDynamic.Should().BeTrue();
        mapping[nameof(SingleItemView.FirstOrder)].SourceExpression.Should().BeNull();
    }

    [Fact]
    public void MapWithFactory_WithDefaultHint_ShouldStoreFactoryType()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<FactoryWithoutHint>();
        var ctx = MakeContext<Storage, View>(services);

        // Act
        ctx.MapWithFactory<FactoryWithoutHint>(v => v.ComputedName);

        // Assert
        var mapping = ctx.Mapping;
        mapping.Should().ContainKey(nameof(View.ComputedName));
        mapping[nameof(View.ComputedName)].FactoryType.Should().Be(typeof(FactoryWithoutHint));
    }

    [Fact]
    public void ProjectionFunctions_GetFirstOrDefault_ShouldReturnValidMethod()
    {
        // Arrange
        var functions = (IProjectionFunctions)Activator.CreateInstance(typeof(ProjectionFunctions<OrderItem, OrderItemView>))!;

        // Act
        var method = functions.GetFirstOrDefault();

        // Assert
        method.Should().NotBeNull();
        method.Name.Should().Be("FirstOrDefault");
        method.ReturnType.Should().Be(typeof(OrderItemView));
    }

    [Fact]
    public void ProjectionFunctions_GetFirstOrDefault_ShouldReturnDifferentMethodThanGetToList()
    {
        // Arrange
        var functions = (IProjectionFunctions)Activator.CreateInstance(typeof(ProjectionFunctions<OrderItem, OrderItemView>))!;

        // Act
        var firstOrDefault = functions.GetFirstOrDefault();
        var toList = functions.GetToList();

        // Assert
        firstOrDefault.Should().NotBeSameAs(toList);
        firstOrDefault.Name.Should().Be("FirstOrDefault");
        toList.Name.Should().Be("ToList");
    }

    private static RqlMapperContext<TStorage, TView> MakeContext<TStorage, TView>(ServiceCollection services)
    {
        var metadataProvider = new MetadataProvider(new PropertyNameProvider(), new MetadataFactory(new Mpt.Rql.Settings.GlobalRqlSettings()));
        return new RqlMapperContext<TStorage, TView>(services.BuildServiceProvider(), metadataProvider);
    }

    // Test entities
    internal class OrderItem
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    internal class OrderItemView
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    internal class Storage
    {
        public string FirstName { get; set; } = string.Empty;
        public List<OrderItem> Orders { get; set; } = [];
    }

    internal class View
    {
        public string ComputedName { get; set; } = string.Empty;
    }

    internal class SingleItemView
    {
        public OrderItemView? FirstOrder { get; set; }
    }

    // Factories
    private class FactoryWithoutHint : IRqlMappingExpressionFactory<Storage>
    {
        public Expression<Func<Storage, object?>> GetStorageExpression()
            => s => s.FirstName;
    }

    private class TakeFirstFactory : IRqlMappingExpressionFactory<Storage>
    {
        public ExpressionFactoryHint Hint => ExpressionFactoryHint.TakeFirst;

        public Expression<Func<Storage, object?>> GetStorageExpression()
            => s => s.Orders;
    }
}
