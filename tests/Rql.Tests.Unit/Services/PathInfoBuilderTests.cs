using System;
using System.Linq.Expressions;
using FluentAssertions;
using Moq;
using Mpt.Rql.Abstractions;
using Mpt.Rql.Abstractions.Configuration;
using Mpt.Rql.Core;
using Mpt.Rql.Core.Metadata;
using Mpt.Rql.Services.Context;
using Mpt.Rql.Services.Filtering;
using Mpt.Rql.Services.Ordering;
using Mpt.Rql.Settings;
using Rql.Tests.Unit.Services.Models;
using Xunit;

namespace Rql.Tests.Unit.Services;

public class PathInfoBuilderTests
{
    private static (FilteringPathInfoBuilder filtering, OrderingPathInfoBuilder ordering) MakeBuilders(
        NavigationStrategy filterNav,
        NavigationStrategy orderNav)
    {
        var actionValidator = new Mock<IActionValidator>();
        actionValidator.Setup(a => a.Validate(It.IsAny<RqlPropertyInfo>(), It.IsAny<Mpt.Rql.RqlActions>())).Returns(true);

        var globalSettings = new GlobalRqlSettings();
        var metadataProvider = new MetadataProvider(new PropertyNameProvider(), new MetadataFactory(globalSettings));
        var builderContext = new BuilderContext();

        var opSettings = new RqlSettings
        {
            Filter = { Navigation = filterNav },
            Ordering = { Navigation = orderNav }
        };

        var filtering = new FilteringPathInfoBuilder(actionValidator.Object, metadataProvider, builderContext, opSettings);
        var ordering = new OrderingPathInfoBuilder(actionValidator.Object, metadataProvider, builderContext, opSettings);
        return (filtering, ordering);
    }

    [Fact]
    public void Build_WithRqlSelf_ReturnsRootInfo()
    {
        // Arrange
        var (filtering, _) = MakeBuilders(NavigationStrategy.Default, NavigationStrategy.Default);
        var param = Expression.Parameter(typeof(Product), "p");

        // Act
        var result = filtering.Build(param, RqlExpression.Self());

        // Assert
        result.IsError.Should().BeFalse();
        result.Value!.PropertyInfo.Should().BeSameAs(RqlPropertyInfo.Root);
        result.Value.Expression.Should().BeSameAs(param);
    }

    [Fact]
    public void Build_WithConstantPath_DefaultNavigation_BuildsMemberAccess()
    {
        // Arrange
        var (filtering, _) = MakeBuilders(NavigationStrategy.Default, NavigationStrategy.Default);
        var param = Expression.Parameter(typeof(Product), "p");

        // Act
        var result = filtering.Build(param, "category.name");

        // Assert
        result.IsError.Should().BeFalse();
        result.Value!.Expression.Should().BeAssignableTo<MemberExpression>();
        result.Value.Expression.Type.Should().Be(typeof(string));
    }

    [Fact]
    public void Build_WithConstantExpression_DefaultNavigation_BuildsMemberAccess()
    {
        // Arrange
        var (filtering, _) = MakeBuilders(NavigationStrategy.Default, NavigationStrategy.Default);
        var param = Expression.Parameter(typeof(Product), "p");

        // Act
        var result = filtering.Build(param, RqlExpression.Constant("category.name"));

        // Assert
        result.IsError.Should().BeFalse();
        result.Value!.Expression.Should().BeAssignableTo<MemberExpression>();
        result.Value.Expression.Type.Should().Be(typeof(string));
    }

    [Fact]
    public void Build_SafeNavigationOn_ValueTypeLeaf_ReturnsNullableType()
    {
        // Arrange
        var (filtering, _) = MakeBuilders(NavigationStrategy.Safe, NavigationStrategy.Default);
        var param = Expression.Parameter(typeof(Product), "p");

        // Act
        var result = filtering.Build(param, "coreCategory.id");

        // Assert
        result.IsError.Should().BeFalse();
        result.Value!.Expression.Type.Should().Be(typeof(int?));
    }

    [Fact]
    public void Build_SafeNavigationOn_ReferenceChain_DoesNotThrowOnNull()
    {
        // Arrange
        var (filtering, _) = MakeBuilders(NavigationStrategy.Safe, NavigationStrategy.Default);
        var param = Expression.Parameter(typeof(Product), "p");
        var built = filtering.Build(param, "category.name");
        built.IsError.Should().BeFalse();

        var body = Expression.Convert(built.Value!.Expression, typeof(object));
        var lambda = Expression.Lambda<Func<Product, object>>(body, param).Compile();
        var instance = new Product { Name = "prod" };

        // Act
        var value = lambda(instance);

        // Assert
        value.Should().BeNull();
    }

    [Fact]
    public void Build_DefaultNavigation_ReferenceChain_ThrowsOnNull()
    {
        // Arrange
        var (filtering, _) = MakeBuilders(NavigationStrategy.Default, NavigationStrategy.Default);
        var param = Expression.Parameter(typeof(Product), "p");
        var built = filtering.Build(param, "category.name");
        built.IsError.Should().BeFalse();

        var body = Expression.Convert(built.Value!.Expression, typeof(object));
        var lambda = Expression.Lambda<Func<Product, object>>(body, param).Compile();
        var instance = new Product { Name = "prod" };

        // Act & Assert
        Assert.Throws<NullReferenceException>(() => lambda(instance));
    }

    [Fact]
    public void Build_Ordering_SafeNavigationOn_DoesNotThrowOnNull()
    {
        // Arrange
        var (_, ordering) = MakeBuilders(NavigationStrategy.Default, NavigationStrategy.Safe);
        var param = Expression.Parameter(typeof(Product), "p");
        var built = ordering.Build(param, "category.name");
        built.IsError.Should().BeFalse();

        var body = Expression.Convert(built.Value!.Expression, typeof(object));
        var lambda = Expression.Lambda<Func<Product, object>>(body, param).Compile();
        var instance = new Product { Name = "prod" };

        // Act
        var value = lambda(instance);

        // Assert
        value.Should().BeNull();
    }

    [Fact]
    public void Build_WithInvalidPath_ReturnsValidationError()
    {
        // Arrange
        var (filtering, _) = MakeBuilders(NavigationStrategy.Safe, NavigationStrategy.Safe);
        var param = Expression.Parameter(typeof(Product), "p");

        // Act
        var result = filtering.Build(param, "category.nonExisting");

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Should().NotBeEmpty();
        result.Errors[0].Message.Should().Be("Invalid property path.");
    }

    [Fact]
    public void Build_IgnoredProperty_ReturnsValidationError()
    {
        // Arrange
        var (filtering, _) = MakeBuilders(NavigationStrategy.Default, NavigationStrategy.Default);
        var param = Expression.Parameter(typeof(Product), "p");

        // Act
        var result = filtering.Build(param, "ignoredCategory");

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Should().NotBeEmpty();
        result.Errors[0].Message.Should().Be("Invalid property path.");
    }
}
