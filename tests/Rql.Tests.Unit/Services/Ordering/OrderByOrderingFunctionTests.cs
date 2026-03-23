using FluentAssertions;
using Moq;
using Mpt.Rql.Abstractions.Configuration;
using Mpt.Rql.Core;
using Mpt.Rql.Core.Metadata;
using Mpt.Rql.Services.Context;
using Mpt.Rql.Services.Ordering;
using Mpt.Rql.Services.Ordering.Functions;
using Mpt.Rql.Settings;
using Rql.Tests.Unit.Services.Models;
using System.Linq.Expressions;
using Xunit;

namespace Rql.Tests.Unit.Services.Ordering;

public class OrderByOrderingFunctionTests
{
    private static OrderByOrderingFunction MakeFunction()
    {
        var actionValidator = new Mock<IActionValidator>();
        actionValidator.Setup(a => a.Validate(It.IsAny<RqlPropertyInfo>(), It.IsAny<Mpt.Rql.RqlActions>())).Returns(true);

        var settings = new RqlSettings();
        var metadataProvider = new MetadataProvider(new PropertyNameProvider(), new MetadataFactory(new GlobalRqlSettings()));
        var builderContext = new BuilderContext();
        var pathBuilder = new OrderingPathInfoBuilder(actionValidator.Object, metadataProvider, builderContext, settings);

        return new OrderByOrderingFunction(pathBuilder);
    }

    [Fact]
    public void Build_ValidArgs_ReturnsStringExpression()
    {
        // orderby(items, name, First, name) → items.Where(i => i.Name=="First").Select(i => i.Name).FirstOrDefault()
        var func = MakeFunction();
        var param = Expression.Parameter(typeof(Product), "p");

        var result = func.Build(param, ["items", "name", "First", "name"]);

        result.IsError.Should().BeFalse();
        result.Value!.Type.Should().Be(typeof(string));
    }

    [Fact]
    public void Build_ValueTypeResult_ReturnsNullableExpression()
    {
        // orderby(items, name, First, id) → items.Where(i => i.Name=="First").Select(i => (int?)i.Id).FirstOrDefault()
        var func = MakeFunction();
        var param = Expression.Parameter(typeof(Product), "p");

        var result = func.Build(param, ["items", "name", "First", "id"]);

        result.IsError.Should().BeFalse();
        result.Value!.Type.Should().Be(typeof(int?));
    }

    [Fact]
    public void Build_MatchingElement_ReturnsCorrectValue()
    {
        var func = MakeFunction();
        var param = Expression.Parameter(typeof(Product), "p");
        var built = func.Build(param, ["items", "name", "Target", "id"]);
        built.IsError.Should().BeFalse();

        var body = Expression.Convert(built.Value!, typeof(object));
        var lambda = Expression.Lambda<Func<Product, object>>(body, param).Compile();
        var product = new Product
        {
            Items =
            [
                new Item { Id = 10, Name = "Other" },
                new Item { Id = 42, Name = "Target" },
                new Item { Id = 99, Name = "Target" }  // second match — should not be returned
            ]
        };

        // Act
        var value = lambda(product);

        // Assert: first match
        value.Should().Be(42);
    }

    [Fact]
    public void Build_NoMatchingElement_ReturnsNull()
    {
        var func = MakeFunction();
        var param = Expression.Parameter(typeof(Product), "p");
        var built = func.Build(param, ["items", "name", "NoSuchName", "id"]);
        built.IsError.Should().BeFalse();

        var body = Expression.Convert(built.Value!, typeof(object));
        var lambda = Expression.Lambda<Func<Product, object>>(body, param).Compile();
        var product = new Product { Items = [new Item { Id = 1, Name = "Other" }] };

        // Act
        var value = lambda(product);

        value.Should().BeNull();
    }

    [Fact]
    public void Build_EmptyCollection_ReturnsNull()
    {
        var func = MakeFunction();
        var param = Expression.Parameter(typeof(Product), "p");
        var built = func.Build(param, ["items", "name", "Anything", "name"]);
        built.IsError.Should().BeFalse();

        var body = Expression.Convert(built.Value!, typeof(object));
        var lambda = Expression.Lambda<Func<Product, object>>(body, param).Compile();
        var product = new Product { Items = [] };

        var value = lambda(product);

        value.Should().BeNull();
    }

    [Fact]
    public void Build_WrongArgCount_ReturnsError()
    {
        var func = MakeFunction();
        var param = Expression.Parameter(typeof(Product), "p");

        var result = func.Build(param, ["items", "name", "val"]);  // only 3 args

        result.IsError.Should().BeTrue();
        result.Errors[0].Message.Should().Contain("4 arguments");
    }

    [Fact]
    public void Build_NonCollectionProperty_ReturnsError()
    {
        var func = MakeFunction();
        var param = Expression.Parameter(typeof(Product), "p");

        var result = func.Build(param, ["name", "anything", "val", "anything"]);  // name is string, not a collection

        result.IsError.Should().BeTrue();
        result.Errors[0].Message.Should().Contain("not a collection property");
    }

    [Fact]
    public void Build_InvalidFilterProperty_ReturnsError()
    {
        var func = MakeFunction();
        var param = Expression.Parameter(typeof(Product), "p");

        var result = func.Build(param, ["items", "nonExistentProp", "val", "name"]);

        result.IsError.Should().BeTrue();
        result.Errors[0].Message.Should().Be("Invalid property path.");
    }

    [Fact]
    public void Build_InvalidResultProperty_ReturnsError()
    {
        var func = MakeFunction();
        var param = Expression.Parameter(typeof(Product), "p");

        var result = func.Build(param, ["items", "name", "val", "nonExistentProp"]);

        result.IsError.Should().BeTrue();
        result.Errors[0].Message.Should().Be("Invalid property path.");
    }

    [Fact]
    public void Build_FilterValueIncompatibleType_ReturnsError()
    {
        // id is int; "not-a-number" cannot be converted to int
        var func = MakeFunction();
        var param = Expression.Parameter(typeof(Product), "p");

        var result = func.Build(param, ["items", "id", "not-a-number", "name"]);

        result.IsError.Should().BeTrue();
        result.Errors[0].Message.Should().Contain("Cannot convert");
    }
}
