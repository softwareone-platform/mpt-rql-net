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
    public void MapWithFactory_WhenFactoryIsRegistered_ShouldMapSuccessfully()
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
        mapping[nameof(View.ComputedName)].SourceExpression.Should().NotBeNull();
        mapping[nameof(View.ComputedName)].FactoryType.Should().Be(typeof(TestExpressionFactory));
    }

    [Fact]
    public void MapWithFactory_WhenFactoryNotRegistered_ShouldThrowRqlMappingException()
    {
        // Arrange
        var services = new ServiceCollection();
        // Note: TestExpressionFactory is NOT registered
        var ctx = MakeContext<Storage, View>(services);

        // Act & Assert
        var act = () => ctx.MapWithFactory<TestExpressionFactory>(v => v.ComputedName);
        act.Should().Throw<RqlMappingException>()
           .WithMessage("*TestExpressionFactory*not found*");
    }

    [Fact]
    public void MapWithFactory_WithConditionalLogic_ShouldReturnDifferentExpressions()
    {
        // Arrange - factory uses if conditions to select expression
        var servicesVerbose = new ServiceCollection();
        servicesVerbose.AddSingleton(new FormatterConfig { UseVerboseFormat = true });
        servicesVerbose.AddTransient<ConditionalFormatter>();
        var ctxVerbose = MakeContext<Product, ProductView>(servicesVerbose);

        var servicesSimple = new ServiceCollection();
        servicesSimple.AddSingleton(new FormatterConfig { UseVerboseFormat = false });
        servicesSimple.AddTransient<ConditionalFormatter>();
        var ctxSimple = MakeContext<Product, ProductView>(servicesSimple);

        // Act
        ctxVerbose.MapWithFactory<ConditionalFormatter>(v => v.DisplayName);
        ctxSimple.MapWithFactory<ConditionalFormatter>(v => v.DisplayName);

        // Assert
        var verboseExpression = (Expression<Func<Product, object?>>)ctxVerbose.Mapping[nameof(ProductView.DisplayName)].SourceExpression;
        var verboseCompiled = verboseExpression.Compile();
        verboseCompiled(new Product { Name = "Widget", Category = "Tools" }).Should().Be("Tools: Widget");

        var simpleExpression = (Expression<Func<Product, object?>>)ctxSimple.Mapping[nameof(ProductView.DisplayName)].SourceExpression;
        var simpleCompiled = simpleExpression.Compile();
        simpleCompiled(new Product { Name = "Widget", Category = "Tools" }).Should().Be("Widget");
    }

    [Fact]
    public void MapWithFactory_WithReferenceTypeProperty_ShouldHandleWithoutConvertWrapper()
    {
        // Arrange - string (reference type) does NOT get wrapped in Convert expression
        // because reference types can be implicitly cast to object? without boxing
        var services = new ServiceCollection();
        services.AddTransient<TestExpressionFactory>();
        var ctx = MakeContext<Storage, View>(services);

        // Verify that the lambda expression body is NOT wrapped in Convert
        Expression<Func<View, object?>> lambdaExpr = v => v.ComputedName;
        lambdaExpr.Body.Should().BeAssignableTo<MemberExpression>("string properties don't need Convert wrapping");
        lambdaExpr.Body.NodeType.Should().Be(ExpressionType.MemberAccess);

        // Act - Expression body will be MemberExpression directly (no Convert wrapper)
        ctx.MapWithFactory<TestExpressionFactory>(v => v.ComputedName);

        // Assert - should successfully map the property
        var mapping = ctx.Mapping;
        mapping.Should().ContainKey(nameof(View.ComputedName));
        mapping[nameof(View.ComputedName)].TargetProperty.Property.Name.Should().Be(nameof(View.ComputedName));
        mapping[nameof(View.ComputedName)].FactoryType.Should().Be(typeof(TestExpressionFactory));
    }

    [Fact]
    public void MapWithFactory_WithValueTypeProperties_RequiresConvertUnwrapping()
    {
        // Arrange - value types (int, bool, DateTime, double) get wrapped in Convert(object?) expression
        // because they need to be boxed when cast to object?
        var services = new ServiceCollection();
        services.AddTransient<IntPropertyFactory>();
        services.AddTransient<BoolPropertyFactory>();
        services.AddTransient<DateTimePropertyFactory>();
        services.AddTransient<DoublePropertyFactory>();
        var ctx = MakeContext<TypedEntity, TypedEntityView>(services);

        // Verify that value type lambda expressions ARE wrapped in Convert
        Expression<Func<TypedEntityView, object?>> intExpr = v => v.Age;
        intExpr.Body.Should().BeOfType<UnaryExpression>("int properties need Convert wrapping for boxing");
        intExpr.Body.NodeType.Should().Be(ExpressionType.Convert);
        ((UnaryExpression)intExpr.Body).Operand.Should().BeAssignableTo<MemberExpression>("Convert wraps the MemberExpression");

        Expression<Func<TypedEntityView, object?>> boolExpr = v => v.IsActive;
        boolExpr.Body.Should().BeOfType<UnaryExpression>("bool properties need Convert wrapping for boxing");

        // Act - Expression body will be UnaryExpression(Convert) wrapping MemberExpression
        // GetTargetProperty must unwrap the Convert to find the MemberExpression
        ctx.MapWithFactory<IntPropertyFactory>(v => v.Age);
        ctx.MapWithFactory<BoolPropertyFactory>(v => v.IsActive);
        ctx.MapWithFactory<DateTimePropertyFactory>(v => v.CreatedAt);
        ctx.MapWithFactory<DoublePropertyFactory>(v => v.Score);

        // Assert - all should successfully unwrap and map
        var mapping = ctx.Mapping;
        mapping.Should().ContainKey(nameof(TypedEntityView.Age));
        mapping.Should().ContainKey(nameof(TypedEntityView.IsActive));
        mapping.Should().ContainKey(nameof(TypedEntityView.CreatedAt));
        mapping.Should().ContainKey(nameof(TypedEntityView.Score));
        
        // Verify factory types are stored
        mapping[nameof(TypedEntityView.Age)].FactoryType.Should().Be(typeof(IntPropertyFactory));
        mapping[nameof(TypedEntityView.IsActive)].FactoryType.Should().Be(typeof(BoolPropertyFactory));
        mapping[nameof(TypedEntityView.CreatedAt)].FactoryType.Should().Be(typeof(DateTimePropertyFactory));
        mapping[nameof(TypedEntityView.Score)].FactoryType.Should().Be(typeof(DoublePropertyFactory));
    }

    [Theory]
    [InlineData(nameof(TypedEntityView.Age), 25)]
    [InlineData(nameof(TypedEntityView.IsActive), true)]
    [InlineData(nameof(TypedEntityView.Score), 98.5)]
    public void MapWithFactory_WithValueTypes_ExecutesMappingCorrectly(string propertyName, object expectedValue)
    {
        // Arrange - Testing that value types (which require Convert unwrapping) map correctly
        var services = new ServiceCollection();
        services.AddTransient<IntPropertyFactory>();
        services.AddTransient<BoolPropertyFactory>();
        services.AddTransient<DoublePropertyFactory>();
        var ctx = MakeContext<TypedEntity, TypedEntityView>(services);

        // Act - Each property expression gets wrapped in Convert(object?) by compiler
        ctx.MapWithFactory<IntPropertyFactory>(v => v.Age);
        ctx.MapWithFactory<BoolPropertyFactory>(v => v.IsActive);
        ctx.MapWithFactory<DoublePropertyFactory>(v => v.Score);

        // Assert - Verify the mapped expression returns correct values after unwrapping
        var mapping = ctx.Mapping;
        mapping.Should().ContainKey(propertyName);
        
        var sourceExpr = (Expression<Func<TypedEntity, object?>>)mapping[propertyName].SourceExpression;
        var compiled = sourceExpr.Compile();
        var entity = new TypedEntity { Age = 25, IsActive = true, Score = 98.5, CreatedAt = DateTime.UtcNow };
        var result = compiled(entity);
        
        result.Should().Be(expectedValue);
    }

    [Fact]
    public void MapWithFactory_WithNullableValueType_RequiresConvertUnwrapping()
    {
        // Arrange - nullable value types (int?) also get wrapped in Convert expression
        // because int? → object? requires boxing
        var services = new ServiceCollection();
        services.AddTransient<NullablePropertyFactory>();
        var ctx = MakeContext<NullableEntity, NullableEntityView>(services);

        // Verify that nullable value type lambda expressions ARE wrapped in Convert
        Expression<Func<NullableEntityView, object?>> nullableExpr = v => v.OptionalAge;
        nullableExpr.Body.Should().BeOfType<UnaryExpression>("nullable int properties need Convert wrapping for boxing");
        nullableExpr.Body.NodeType.Should().Be(ExpressionType.Convert);

        // Act - Expression body will be UnaryExpression(Convert) wrapping MemberExpression
        ctx.MapWithFactory<NullablePropertyFactory>(v => v.OptionalAge);

        // Assert - Should successfully unwrap and map
        var mapping = ctx.Mapping;
        mapping.Should().ContainKey(nameof(NullableEntityView.OptionalAge));
        mapping[nameof(NullableEntityView.OptionalAge)].FactoryType.Should().Be(typeof(NullablePropertyFactory));
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
        public Expression<Func<Storage, object?>> GetMappingExpression()
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

        public Expression<Func<Product, object?>> GetMappingExpression()
        {
            // .NET if condition to select which expression to return
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
        public DateTime CreatedAt { get; set; }
        public double Score { get; set; }
    }

    private class TypedEntityView
    {
        public int Age { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public double Score { get; set; }
    }

    private class IntPropertyFactory : IRqlMappingExpressionFactory<TypedEntity>
    {
        public Expression<Func<TypedEntity, object?>> GetMappingExpression()
        {
            return e => e.Age;
        }
    }

    private class BoolPropertyFactory : IRqlMappingExpressionFactory<TypedEntity>
    {
        public Expression<Func<TypedEntity, object?>> GetMappingExpression()
        {
            return e => e.IsActive;
        }
    }

    private class DateTimePropertyFactory : IRqlMappingExpressionFactory<TypedEntity>
    {
        public Expression<Func<TypedEntity, object?>> GetMappingExpression()
        {
            return e => e.CreatedAt;
        }
    }

    private class DoublePropertyFactory : IRqlMappingExpressionFactory<TypedEntity>
    {
        public Expression<Func<TypedEntity, object?>> GetMappingExpression()
        {
            return e => e.Score;
        }
    }

    private class NullableEntity
    {
        public int? OptionalAge { get; set; }
    }

    private class NullableEntityView
    {
        public int? OptionalAge { get; set; }
    }

    private class NullablePropertyFactory : IRqlMappingExpressionFactory<NullableEntity>
    {
        public Expression<Func<NullableEntity, object?>> GetMappingExpression()
        {
            return e => e.OptionalAge;
        }
    }
}
