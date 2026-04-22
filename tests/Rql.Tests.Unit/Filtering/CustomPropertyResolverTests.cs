using FluentAssertions;
using Moq;
using Mpt.Rql;
using Mpt.Rql.Abstractions;
using Mpt.Rql.Core;
using Mpt.Rql.Services.Context;
using Mpt.Rql.Services.Filtering;
using Rql.Tests.Common.Factory;
using System.Linq.Expressions;
using System.Text.Json;
using Xunit;

namespace Rql.Tests.Unit.Filtering;

public class CustomPropertyResolverTests
{
    private static FilteringPathInfoBuilder CreateSut(params IRqlCustomPropertyResolver[] resolvers)
    {
        return new FilteringPathInfoBuilder(
            new SimpleActionValidator(),
            MetadataProviderFactory.Internal(),
            new BuilderContext(),
            RqlSettingsFactory.Default(),
            resolvers);
    }

    private static FilteringPathInfoBuilder CreateSutWithNullResolvers()
    {
        return new FilteringPathInfoBuilder(
            new SimpleActionValidator(),
            MetadataProviderFactory.Internal(),
            new BuilderContext(),
            RqlSettingsFactory.Default(),
            null);
    }

    [Fact]
    public void Build_WithResolver_WhenMetadataFails_CallsResolverAndReturnsResult()
    {
        // Arrange
        var resolverMock = new Mock<IRqlCustomPropertyResolver>();
        var expectedExpression = Expression.Constant("resolved", typeof(string));
        resolverMock
            .Setup(r => r.TryResolve(It.IsAny<Expression>(), "dynamicProp", out It.Ref<Expression>.IsAny, out It.Ref<IRqlPropertyInfo>.IsAny))
            .Returns((Expression parent, string name, out Expression expr, out IRqlPropertyInfo info) =>
            {
                expr = expectedExpression;
                info = CreateSyntheticPropertyInfo(name);
                return true;
            });

        var sut = CreateSut(resolverMock.Object);
        var root = Expression.Parameter(typeof(EntityWithJson), "e");

        // Act — "jsonProp.dynamicProp" : first segment resolves via metadata, second via resolver
        var result = sut.Build(root, "jsonProp.dynamicProp");

        // Assert
        result.IsError.Should().BeFalse();
        result.Value!.PropertyInfo.Name.Should().Be("dynamicProp");
        result.Value.Expression.Should().BeSameAs(expectedExpression);
        resolverMock.Verify(r => r.TryResolve(It.IsAny<Expression>(), "dynamicProp", out It.Ref<Expression>.IsAny, out It.Ref<IRqlPropertyInfo>.IsAny), Times.Once);
    }

    [Fact]
    public void Build_WithResolver_WhenResolverReturnsFalse_ReturnsInvalidPropertyPathError()
    {
        // Arrange
        var resolverMock = new Mock<IRqlCustomPropertyResolver>();
        resolverMock
            .Setup(r => r.TryResolve(It.IsAny<Expression>(), It.IsAny<string>(), out It.Ref<Expression>.IsAny, out It.Ref<IRqlPropertyInfo>.IsAny))
            .Returns(false);

        var sut = CreateSut(resolverMock.Object);
        var root = Expression.Parameter(typeof(EntityWithJson), "e");

        // Act
        var result = sut.Build(root, "jsonProp.unknownKey");

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Message.Contains("Invalid property path"));
    }

    [Fact]
    public void Build_WithNoResolvers_UnknownProperty_ReturnsInvalidPropertyPathError()
    {
        // Arrange — empty array of resolvers
        var sut = CreateSut();
        var root = Expression.Parameter(typeof(EntityWithJson), "e");

        // Act
        var result = sut.Build(root, "jsonProp.unknownKey");

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Message.Contains("Invalid property path"));
    }

    [Fact]
    public void Build_WithNullResolvers_UnknownProperty_ReturnsError()
    {
        // Arrange — null resolvers (matches DI behavior when none are registered)
        var sut = CreateSutWithNullResolvers();
        var root = Expression.Parameter(typeof(EntityWithJson), "e");

        // Act
        var result = sut.Build(root, "jsonProp.unknownKey");

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Message.Contains("Invalid property path"));
    }

    [Fact]
    public void Build_WithMultipleResolvers_FirstMatchWins()
    {
        // Arrange
        var resolver1 = new Mock<IRqlCustomPropertyResolver>();
        resolver1
            .Setup(r => r.TryResolve(It.IsAny<Expression>(), "dynamicProp", out It.Ref<Expression>.IsAny, out It.Ref<IRqlPropertyInfo>.IsAny))
            .Returns((Expression parent, string name, out Expression expr, out IRqlPropertyInfo info) =>
            {
                expr = Expression.Constant("from-resolver-1", typeof(string));
                info = CreateSyntheticPropertyInfo("fromResolver1");
                return true;
            });

        var resolver2 = new Mock<IRqlCustomPropertyResolver>();

        var sut = CreateSut(resolver1.Object, resolver2.Object);
        var root = Expression.Parameter(typeof(EntityWithJson), "e");

        // Act
        var result = sut.Build(root, "jsonProp.dynamicProp");

        // Assert
        result.IsError.Should().BeFalse();
        result.Value!.PropertyInfo.Name.Should().Be("fromResolver1");
        resolver1.Verify(r => r.TryResolve(It.IsAny<Expression>(), "dynamicProp", out It.Ref<Expression>.IsAny, out It.Ref<IRqlPropertyInfo>.IsAny), Times.Once);
        resolver2.Verify(r => r.TryResolve(It.IsAny<Expression>(), It.IsAny<string>(), out It.Ref<Expression>.IsAny, out It.Ref<IRqlPropertyInfo>.IsAny), Times.Never);
    }

    [Fact]
    public void Build_WithMultipleResolvers_FallsThrough_WhenFirstReturnsFalse()
    {
        // Arrange
        var resolver1 = new Mock<IRqlCustomPropertyResolver>();
        resolver1
            .Setup(r => r.TryResolve(It.IsAny<Expression>(), It.IsAny<string>(), out It.Ref<Expression>.IsAny, out It.Ref<IRqlPropertyInfo>.IsAny))
            .Returns(false);

        var resolver2 = new Mock<IRqlCustomPropertyResolver>();
        resolver2
            .Setup(r => r.TryResolve(It.IsAny<Expression>(), "dynamicProp", out It.Ref<Expression>.IsAny, out It.Ref<IRqlPropertyInfo>.IsAny))
            .Returns((Expression parent, string name, out Expression expr, out IRqlPropertyInfo info) =>
            {
                expr = Expression.Constant("from-resolver-2", typeof(string));
                info = CreateSyntheticPropertyInfo("fromResolver2");
                return true;
            });

        var sut = CreateSut(resolver1.Object, resolver2.Object);
        var root = Expression.Parameter(typeof(EntityWithJson), "e");

        // Act
        var result = sut.Build(root, "jsonProp.dynamicProp");

        // Assert
        result.IsError.Should().BeFalse();
        result.Value!.PropertyInfo.Name.Should().Be("fromResolver2");
    }

    [Fact]
    public void Build_NestedPathAfterResolvedProperty_ReturnsNestedAccessError()
    {
        // Arrange — resolver handles "dynamicProp", but then "nested" follows
        var resolverMock = new Mock<IRqlCustomPropertyResolver>();
        resolverMock
            .Setup(r => r.TryResolve(It.IsAny<Expression>(), "dynamicProp", out It.Ref<Expression>.IsAny, out It.Ref<IRqlPropertyInfo>.IsAny))
            .Returns((Expression parent, string name, out Expression expr, out IRqlPropertyInfo info) =>
            {
                expr = Expression.Constant("resolved", typeof(string));
                info = CreateSyntheticPropertyInfo(name);
                return true;
            });

        var sut = CreateSut(resolverMock.Object);
        var root = Expression.Parameter(typeof(EntityWithJson), "e");

        // Act — "jsonProp.dynamicProp.nested" should fail on "nested"
        var result = sut.Build(root, "jsonProp.dynamicProp.nested");

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Message.Contains("Nested property access"));
    }

    [Fact]
    public void Build_ResolverPropertyWithoutFilterAction_ReturnsFilteringNotPermittedError()
    {
        // Arrange — resolver returns a property with Select-only actions
        var resolverMock = new Mock<IRqlCustomPropertyResolver>();
        resolverMock
            .Setup(r => r.TryResolve(It.IsAny<Expression>(), "dynamicProp", out It.Ref<Expression>.IsAny, out It.Ref<IRqlPropertyInfo>.IsAny))
            .Returns((Expression parent, string name, out Expression expr, out IRqlPropertyInfo info) =>
            {
                expr = Expression.Constant("resolved", typeof(string));
                info = CreateSyntheticPropertyInfo(name, RqlActions.Select); // no Filter
                return true;
            });

        var sut = CreateSut(resolverMock.Object);
        var root = Expression.Parameter(typeof(EntityWithJson), "e");

        // Act
        var result = sut.Build(root, "jsonProp.dynamicProp");

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Message.Contains("Filtering is not permitted"));
    }

    [Fact]
    public void Build_StandardClrPath_WithResolversRegistered_DoesNotCallResolver()
    {
        // Arrange — resolver is registered but "name" is a standard CLR property
        var resolverMock = new Mock<IRqlCustomPropertyResolver>();

        var sut = CreateSut(resolverMock.Object);
        var root = Expression.Parameter(typeof(EntityWithJson), "e");

        // Act
        var result = sut.Build(root, "name");

        // Assert
        result.IsError.Should().BeFalse();
        result.Value!.PropertyInfo.Name.Should().Be("name");
        resolverMock.Verify(r => r.TryResolve(It.IsAny<Expression>(), It.IsAny<string>(), out It.Ref<Expression>.IsAny, out It.Ref<IRqlPropertyInfo>.IsAny), Times.Never);
    }

    [Fact]
    public void Build_UnknownFirstSegment_CallsResolver()
    {
        // Arrange — "unknownRoot" doesn't exist on the entity, resolver should be tried
        var resolverMock = new Mock<IRqlCustomPropertyResolver>();
        resolverMock
            .Setup(r => r.TryResolve(It.IsAny<Expression>(), "unknownRoot", out It.Ref<Expression>.IsAny, out It.Ref<IRqlPropertyInfo>.IsAny))
            .Returns(false);

        var sut = CreateSut(resolverMock.Object);
        var root = Expression.Parameter(typeof(EntityWithJson), "e");

        // Act
        var result = sut.Build(root, "unknownRoot");

        // Assert
        result.IsError.Should().BeTrue();
        resolverMock.Verify(r => r.TryResolve(It.IsAny<Expression>(), "unknownRoot", out It.Ref<Expression>.IsAny, out It.Ref<IRqlPropertyInfo>.IsAny), Times.Once);
    }

    private static IRqlPropertyInfo CreateSyntheticPropertyInfo(string name, RqlActions actions = RqlActions.Filter)
    {
        var mock = new Mock<IRqlPropertyInfo>();
        mock.Setup(p => p.Name).Returns(name);
        mock.Setup(p => p.Type).Returns(RqlPropertyType.Primitive);
        mock.Setup(p => p.Actions).Returns(actions);
        mock.Setup(p => p.Operators).Returns(RqlOperators.Eq | RqlOperators.Ne);
        mock.Setup(p => p.IsNullable).Returns(true);
        mock.Setup(p => p.Mode).Returns(RqlPropertyMode.Default);
        return mock.Object;
    }

    // Test entity with a JsonElement property that metadata can resolve,
    // but whose child properties require a custom resolver
    public class EntityWithJson
    {
        public JsonElement? JsonProp { get; set; }
        public string Name { get; set; } = null!;
    }
}
