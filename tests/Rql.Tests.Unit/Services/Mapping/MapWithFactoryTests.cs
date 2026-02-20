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
}
