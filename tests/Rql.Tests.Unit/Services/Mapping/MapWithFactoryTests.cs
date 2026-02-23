using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Mpt.Rql;
using Mpt.Rql.Core.Metadata;
using Mpt.Rql.Services.Mapping;
using System.Linq.Expressions;
using Xunit;

namespace Rql.Tests.Unit.Services.Mapping;

public class MapWithFactoryTests
{
    [Fact]
    public void MapWithFactory_ShouldStoreFactoryType()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<TestExpressionFactory>();
        var ctx = MakeContext<Storage, View>(services);

        // Act
        ctx.MapWithFactory<TestExpressionFactory>(v => v.ComputedName);

        // Assert
        var mapping = ctx.Mapping;
        mapping.Should().ContainKey(nameof(View.ComputedName));
        mapping[nameof(View.ComputedName)].IsDynamic.Should().BeTrue();
        mapping[nameof(View.ComputedName)].TargetProperty.Property.Name.Should().Be(nameof(View.ComputedName));
        mapping[nameof(View.ComputedName)].SourceExpression.Should().BeNull("factory resolution is delayed until mapping time");
        mapping[nameof(View.ComputedName)].FactoryType.Should().Be(typeof(TestExpressionFactory));
    }

    [Fact]
    public void MapWithFactory_WithConditionalLogic_ShouldSupportDifferentConfigurations()
    {
        // Arrange - factory uses DI to conditionally return different expressions
        var servicesVerbose = new ServiceCollection();
        servicesVerbose.AddSingleton(new FormatterConfig { UseVerboseFormat = true });
        servicesVerbose.AddTransient<ConditionalFormatter>();

        var servicesSimple = new ServiceCollection();
        servicesSimple.AddSingleton(new FormatterConfig { UseVerboseFormat = false });
        servicesSimple.AddTransient<ConditionalFormatter>();

        // Act - Verify factories return different expressions based on configuration
        var verboseFactory = servicesVerbose.BuildServiceProvider().GetRequiredService<ConditionalFormatter>();
        var verboseExpression = verboseFactory.GetStorageExpression();
        var verboseCompiled = verboseExpression.Compile();
        
        var simpleFactory = servicesSimple.BuildServiceProvider().GetRequiredService<ConditionalFormatter>();
        var simpleExpression = simpleFactory.GetStorageExpression();
        var simpleCompiled = simpleExpression.Compile();

        // Assert
        verboseCompiled(new Product { Name = "Widget", Category = "Tools" }).Should().Be("Tools: Widget");
        simpleCompiled(new Product { Name = "Widget", Category = "Tools" }).Should().Be("Widget");
    }

    [Theory]
    [InlineData(nameof(TypedEntityView.Age), typeof(IntPropertyFactory))]
    [InlineData(nameof(TypedEntityView.IsActive), typeof(BoolPropertyFactory))]
    [InlineData(nameof(TypedEntityView.Score), typeof(DoublePropertyFactory))]
    public void MapWithFactory_WithValueTypes_ShouldStoreFactoryType(string propertyName, Type factoryType)
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<IntPropertyFactory>();
        services.AddTransient<BoolPropertyFactory>();
        services.AddTransient<DoublePropertyFactory>();
        var ctx = MakeContext<TypedEntity, TypedEntityView>(services);

        // Act
        ctx.MapWithFactory<IntPropertyFactory>(v => v.Age);
        ctx.MapWithFactory<BoolPropertyFactory>(v => v.IsActive);
        ctx.MapWithFactory<DoublePropertyFactory>(v => v.Score);

        // Assert
        var mapping = ctx.Mapping;
        mapping.Should().ContainKey(propertyName);
        mapping[propertyName].FactoryType.Should().Be(factoryType);
        mapping[propertyName].SourceExpression.Should().BeNull("factory resolution is delayed until mapping time");
    }

    [Fact]
    public void MapWithFactory_StoresFactoryType_WhileOtherMappingsDoNot()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<TestExpressionFactory>();
        var ctx = MakeContext<Storage, View>(services);

        // Act - Use different mapping methods
        ctx.MapWithFactory<TestExpressionFactory>(v => v.ComputedName);
        ctx.MapStatic<string, string>(v => v.AdditionalInfo, s => s.FirstName);

        // Assert
        var mapping = ctx.Mapping;
        
        // MapWithFactory should store the factory type
        mapping[nameof(View.ComputedName)].FactoryType.Should().Be(typeof(TestExpressionFactory));
        
        // Other mapping methods should have null FactoryType
        mapping[nameof(View.AdditionalInfo)].FactoryType.Should().BeNull();
    }

    private static RqlMapperContext<TStorage, TView> MakeContext<TStorage, TView>(ServiceCollection services)
    {
        var metadataProvider = new MetadataProvider(new PropertyNameProvider(), new MetadataFactory(new Mpt.Rql.Settings.GlobalRqlSettings()));
        return new RqlMapperContext<TStorage, TView>(services.BuildServiceProvider(), metadataProvider);
    }

    private class Storage
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
    }

    private class View
    {
        public string ComputedName { get; set; } = string.Empty;
        public string AdditionalInfo { get; set; } = string.Empty;
    }

    private class TestExpressionFactory : IRqlMappingExpressionFactory<Storage>
    {
        public Expression<Func<Storage, object?>> GetStorageExpression()
        {
            return s => s.FirstName + " " + s.LastName;
        }
    }

    private class Product
    {
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
    }

    private class ProductView
    {
        public string DisplayName { get; set; } = string.Empty;
    }

    private class FormatterConfig
    {
        public bool UseVerboseFormat { get; set; }
    }

    private class ConditionalFormatter : IRqlMappingExpressionFactory<Product>
    {
        private readonly FormatterConfig _config;

        public ConditionalFormatter(FormatterConfig config)
        {
            _config = config;
        }

        public Expression<Func<Product, object?>> GetStorageExpression()
        {
            if (_config.UseVerboseFormat)
            {
                return p => p.Category + ": " + p.Name;
            }
            else
            {
                return p => p.Name;
            }
        }
    }

    private class TypedEntity
    {
        public int Age { get; set; }
        public bool IsActive { get; set; }
        public double Score { get; set; }
    }

    private class TypedEntityView
    {
        public int Age { get; set; }
        public bool IsActive { get; set; }
        public double Score { get; set; }
    }

    private class IntPropertyFactory : IRqlMappingExpressionFactory<TypedEntity>
    {
        public Expression<Func<TypedEntity, object?>> GetStorageExpression()
        {
            return e => e.Age;
        }
    }

    private class BoolPropertyFactory : IRqlMappingExpressionFactory<TypedEntity>
    {
        public Expression<Func<TypedEntity, object?>> GetStorageExpression()
        {
            return e => e.IsActive;
        }
    }

    private class DoublePropertyFactory : IRqlMappingExpressionFactory<TypedEntity>
    {
        public Expression<Func<TypedEntity, object?>> GetStorageExpression()
        {
            return e => e.Score;
        }
    }
}
