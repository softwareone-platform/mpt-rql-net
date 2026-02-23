using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Mpt.Rql;
using Mpt.Rql.Services.Mapping;
using Rql.Tests.Integration.Core;
using System.Linq.Expressions;
using Xunit;

namespace Rql.Tests.Integration.Tests.Extensibility;

public class MapWithFactoryTests
{
    [Fact]
    public void MapWithFactory_ShouldResolveFactoryAndMap()
    {
        // Arrange
        var rql = RqlFactory.Make<Product, ProductViewWithFactory>(
            services =>
            {
                services.AddTransient<FullNameFactory>();
            },
            config => config.ScanForMappers(typeof(MapWithFactoryTests).Assembly));

        var testData = ProductRepository.Query();

        // Act
        var result = rql.Transform(testData, new RqlRequest { Select = "Id,FullName" });

        // Assert
        Assert.True(result.IsSuccess);
        var mapped = result.Query.ToList();
        
        mapped.Should().HaveCountGreaterThan(0);
        mapped[0].FullName.Should().Be($"Product: {mapped[0].Id}");
    }

    [Fact]
    public void MapWithFactory_WithConditionalLogic_ShouldUseDifferentExpressions()
    {
        // Arrange - Verbose formatter
        var rqlVerbose = RqlFactory.Make<Product, ProductViewWithConditionalFactory>(
            services =>
            {
                services.AddSingleton(new FormatterSettings { Verbose = true });
                services.AddTransient<ConditionalFormatterFactory>();
            },
            config => config.ScanForMappers(typeof(MapWithFactoryTests).Assembly));

        var testData = ProductRepository.Query();

        // Act
        var resultVerbose = rqlVerbose.Transform(testData, new RqlRequest { Select = "Id,DisplayName" });

        // Assert
        Assert.True(resultVerbose.IsSuccess);
        var mappedVerbose = resultVerbose.Query.First();
        mappedVerbose.DisplayName.Should().Contain(mappedVerbose.Id.ToString());
        mappedVerbose.DisplayName.Should().StartWith("Product #");
    }

    [Fact]
    public void MapWithFactory_WithSimpleFormatter_ShouldUseSimpleExpression()
    {
        // Arrange - Simple formatter
        var rqlSimple = RqlFactory.Make<Product, ProductViewWithConditionalFactory>(
            services =>
            {
                services.AddSingleton(new FormatterSettings { Verbose = false });
                services.AddTransient<ConditionalFormatterFactory>();
            },
            config => config.ScanForMappers(typeof(MapWithFactoryTests).Assembly));

        var testData = ProductRepository.Query();

        // Act
        var resultSimple = rqlSimple.Transform(testData, new RqlRequest { Select = "Id,DisplayName" });

        // Assert
        Assert.True(resultSimple.IsSuccess);
        var mappedSimple = resultSimple.Query.First();
        mappedSimple.DisplayName.Should().NotStartWith("Product #");
        mappedSimple.DisplayName.Should().Be(mappedSimple.Id.ToString());
    }

    [Fact]
    public void MapWithFactory_WhenFactoryNotRegistered_ShouldThrowException()
    {
        // Arrange - Factory NOT registered
        var rql = RqlFactory.Make<Product, ProductViewWithFactory>(
            services =>
            {
                // FullNameFactory is intentionally NOT registered
            },
            config => config.ScanForMappers(typeof(MapWithFactoryTests).Assembly));

        var testData = ProductRepository.Query();

        // Act
        var exception = Assert.Throws<RqlMappingException>(() =>
            rql.Transform(testData, new RqlRequest { Select = "Id,FullName" }));
        
        // Assert
        exception.Message.Should().Contain("FullNameFactory");
        exception.Message.Should().Contain("not found");
    }

    [Fact]
    public void MapWithFactory_WithDependencyInjection_ShouldUseInjectedService()
    {
        // Arrange
        var rql = RqlFactory.Make<Product, ProductViewWithDIFactory>(
            services =>
            {
                services.AddSingleton(new PrefixSettings { Prefix = "Item" });
                services.AddTransient<DIAwareFactory>();
            },
            config => config.ScanForMappers(typeof(MapWithFactoryTests).Assembly));

        var testData = ProductRepository.Query().Take(2);

        // Act
        var result = rql.Transform(testData, new RqlRequest { Select = "Id,FormattedName" });

        // Assert
        Assert.True(result.IsSuccess);
        var mapped = result.Query.ToList();
        
        mapped.Should().HaveCount(2);
        mapped.All(x => x.FormattedName.StartsWith("Item: ")).Should().BeTrue();
    }
}

// Test Models
internal class ProductViewWithFactory
{
    public int Id { get; set; }
    public string FullName { get; set; } = null!;
}

internal class ProductViewWithConditionalFactory
{
    public int Id { get; set; }
    public string DisplayName { get; set; } = null!;
}

internal class ProductViewWithDIFactory
{
    public int Id { get; set; }
    public string FormattedName { get; set; } = null!;
}

// Mappers
internal class ProductToFactoryViewMapper : IRqlMapper<Product, ProductViewWithFactory>
{
    public void MapEntity(IRqlMapperContext<Product, ProductViewWithFactory> context)
    {
        context.MapStatic(v => v.Id, s => s.Id);
        context.MapWithFactory<FullNameFactory>(v => v.FullName);
    }
}

internal class ProductToConditionalViewMapper : IRqlMapper<Product, ProductViewWithConditionalFactory>
{
    public void MapEntity(IRqlMapperContext<Product, ProductViewWithConditionalFactory> context)
    {
        context.MapStatic(v => v.Id, s => s.Id);
        context.MapWithFactory<ConditionalFormatterFactory>(v => v.DisplayName);
    }
}

internal class ProductToDIFactoryViewMapper : IRqlMapper<Product, ProductViewWithDIFactory>
{
    public void MapEntity(IRqlMapperContext<Product, ProductViewWithDIFactory> context)
    {
        context.MapStatic(v => v.Id, s => s.Id);
        context.MapWithFactory<DIAwareFactory>(v => v.FormattedName);
    }
}

// Factories
internal class FullNameFactory : IRqlMappingExpressionFactory<Product>
{
    public Expression<Func<Product, object?>> GetStorageExpression()
    {
        return p => "Product: " + p.Id.ToString();
    }
}

internal class FormatterSettings
{
    public bool Verbose { get; set; }
}

internal class ConditionalFormatterFactory : IRqlMappingExpressionFactory<Product>
{
    private readonly FormatterSettings _settings;

    public ConditionalFormatterFactory(FormatterSettings settings)
    {
        _settings = settings;
    }

    public Expression<Func<Product, object?>> GetStorageExpression()
    {
        if (_settings.Verbose)
        {
            return p => "Product #" + p.Id.ToString();
        }
        else
        {
            return p => p.Id.ToString();
        }
    }
}

internal class PrefixSettings
{
    public string Prefix { get; set; } = string.Empty;
}

internal class DIAwareFactory : IRqlMappingExpressionFactory<Product>
{
    private readonly PrefixSettings _settings;

    public DIAwareFactory(PrefixSettings settings)
    {
        _settings = settings;
    }

    public Expression<Func<Product, object?>> GetStorageExpression()
    {
        var prefix = _settings.Prefix;
        return p => prefix + ": " + p.Name;
    }
}
