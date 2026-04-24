using FluentAssertions;
using Moq;
using Mpt.Rql;
using Mpt.Rql.Abstractions;
using Mpt.Rql.Abstractions.Exception;
using Mpt.Rql.Core;
using Mpt.Rql.Core.Metadata;
using Mpt.Rql.Services.Context;
using Mpt.Rql.Services.Filtering;
using Rql.Tests.Common.Factory;
using System.Linq.Expressions;
using System.Text.Json;
using Xunit;

namespace Rql.Tests.Unit.Filtering;

public class CustomPropertyResolverTests
{
    private static FilteringPathInfoBuilder CreateSut(Mock<IExternalServiceAccessor>? externalServicesMock = null)
    {
        externalServicesMock ??= new Mock<IExternalServiceAccessor>();
        return new FilteringPathInfoBuilder(
            new SimpleActionValidator(),
            MetadataProviderFactory.Internal(),
            new BuilderContext(),
            RqlSettingsFactory.Default(),
            externalServicesMock.Object);
    }

    private static Mock<IExternalServiceAccessor> RegisterResolver<TResolver>(IRqlCustomPropertyResolver resolver)
        where TResolver : IRqlCustomPropertyResolver
    {
        var mock = new Mock<IExternalServiceAccessor>();
        mock.Setup(s => s.GetService(typeof(TResolver))).Returns(resolver);
        return mock;
    }

    [Fact]
    public void Build_WithCustomResolverOnParent_WhenChildSegmentFailsMetadata_CallsResolverAndReturnsResult()
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

        var sut = CreateSut(RegisterResolver<ITestResolver>(resolverMock.Object));
        var root = Expression.Parameter(typeof(EntityWithJson), "e");

        // Act — "jsonProp.dynamicProp" : first segment resolves via metadata (carries CustomResolver), second via resolver
        var result = sut.Build(root, "jsonProp.dynamicProp");

        // Assert
        result.IsError.Should().BeFalse();
        result.Value!.PropertyInfo.Name.Should().Be("dynamicProp");
        result.Value.Expression.Should().BeSameAs(expectedExpression);
        resolverMock.Verify(r => r.TryResolve(It.IsAny<Expression>(), "dynamicProp", out It.Ref<Expression>.IsAny, out It.Ref<IRqlPropertyInfo>.IsAny), Times.Once);
    }

    [Fact]
    public void Build_WithCustomResolver_WhenResolverReturnsFalse_ReturnsInvalidPropertyPathError()
    {
        // Arrange
        var resolverMock = new Mock<IRqlCustomPropertyResolver>();
        resolverMock
            .Setup(r => r.TryResolve(It.IsAny<Expression>(), It.IsAny<string>(), out It.Ref<Expression>.IsAny, out It.Ref<IRqlPropertyInfo>.IsAny))
            .Returns(false);

        var sut = CreateSut(RegisterResolver<ITestResolver>(resolverMock.Object));
        var root = Expression.Parameter(typeof(EntityWithJson), "e");

        // Act
        var result = sut.Build(root, "jsonProp.unknownKey");

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Message.Contains("Invalid property path"));
    }

    [Fact]
    public void Build_PropertyWithoutCustomResolver_ChildSegmentFails_ReturnsInvalidPropertyPathError()
    {
        // Arrange — plainJson has no CustomResolver attribute, so child segment must fail
        var resolverMock = new Mock<IRqlCustomPropertyResolver>();
        var sut = CreateSut(RegisterResolver<ITestResolver>(resolverMock.Object));
        var root = Expression.Parameter(typeof(EntityWithJson), "e");

        // Act
        var result = sut.Build(root, "plainJson.dynamicProp");

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Message.Contains("Invalid property path"));
        resolverMock.Verify(r => r.TryResolve(It.IsAny<Expression>(), It.IsAny<string>(), out It.Ref<Expression>.IsAny, out It.Ref<IRqlPropertyInfo>.IsAny), Times.Never);
    }

    [Fact]
    public void Build_NestedPathAfterResolverBoundary_ForwardsFullDottedKeyToResolver()
    {
        // Segments after the resolver-annotated property are concatenated and handed to the resolver
        // in one call. The resolver decides whether to translate the dotted key (e.g. into a
        // JSON_VALUE('$.a.b.c') call) or reject it.

        // Arrange
        var resolverMock = new Mock<IRqlCustomPropertyResolver>();
        var leaf = Expression.Constant("resolved", typeof(string));
        resolverMock
            .Setup(r => r.TryResolve(It.IsAny<Expression>(), "a.b.c", out It.Ref<Expression>.IsAny, out It.Ref<IRqlPropertyInfo>.IsAny))
            .Returns((Expression parent, string name, out Expression expr, out IRqlPropertyInfo info) =>
            {
                expr = leaf;
                info = CreateSyntheticPropertyInfo(name);
                return true;
            });

        var sut = CreateSut(RegisterResolver<ITestResolver>(resolverMock.Object));
        var root = Expression.Parameter(typeof(EntityWithJson), "e");

        // Act — resolver should receive "a.b.c" as a single dotted key
        var result = sut.Build(root, "jsonProp.a.b.c");

        // Assert
        result.IsError.Should().BeFalse();
        result.Value!.PropertyInfo.Name.Should().Be("a.b.c");
        result.Value.Expression.Should().BeSameAs(leaf);
        resolverMock.Verify(
            r => r.TryResolve(It.IsAny<Expression>(), "a.b.c", out It.Ref<Expression>.IsAny, out It.Ref<IRqlPropertyInfo>.IsAny),
            Times.Once);
    }

    [Fact]
    public void Build_NestedPath_ResolverReceivesCarrierExpressionAsParent()
    {
        // The parent expression handed to the resolver is the access to the CustomResolver-annotated
        // property itself (e.g. `e.JsonProp`) — not any deeper node. That's the anchor the resolver
        // needs to emit e.g. JSON_VALUE(e.JsonProp, '$.a.b').

        // Arrange
        Expression? capturedParent = null;
        var resolverMock = new Mock<IRqlCustomPropertyResolver>();
        resolverMock
            .Setup(r => r.TryResolve(It.IsAny<Expression>(), It.IsAny<string>(), out It.Ref<Expression>.IsAny, out It.Ref<IRqlPropertyInfo>.IsAny))
            .Returns((Expression parent, string name, out Expression expr, out IRqlPropertyInfo info) =>
            {
                capturedParent = parent;
                expr = Expression.Constant("resolved", typeof(string));
                info = CreateSyntheticPropertyInfo(name);
                return true;
            });

        var sut = CreateSut(RegisterResolver<ITestResolver>(resolverMock.Object));
        var root = Expression.Parameter(typeof(EntityWithJson), "e");

        // Act
        var result = sut.Build(root, "jsonProp.a.b");

        // Assert — parent is `e.JsonProp` (the CustomResolver-annotated property access)
        result.IsError.Should().BeFalse();
        capturedParent.Should().BeAssignableTo<MemberExpression>();
        var memberAccess = (MemberExpression)capturedParent!;
        memberAccess.Member.Name.Should().Be(nameof(EntityWithJson.JsonProp));
        memberAccess.Expression.Should().BeSameAs(root);
    }

    [Fact]
    public void Build_NestedPath_ValidationError_ReferencesFullPath()
    {
        // When the resolver returns a synthetic property whose actions fail validation (e.g. Filter
        // not permitted), the error location must reference the FULL dotted path the user sent —
        // not the prefix at which the resolver boundary was crossed. Otherwise the error would
        // mislead clients into thinking the issue is at a shorter path than what they actually sent.

        // Arrange — resolver accepts the dotted key but returns Select-only actions
        var resolverMock = new Mock<IRqlCustomPropertyResolver>();
        resolverMock
            .Setup(r => r.TryResolve(It.IsAny<Expression>(), "a.b.c", out It.Ref<Expression>.IsAny, out It.Ref<IRqlPropertyInfo>.IsAny))
            .Returns((Expression parent, string name, out Expression expr, out IRqlPropertyInfo info) =>
            {
                expr = Expression.Constant("resolved", typeof(string));
                info = CreateSyntheticPropertyInfo(name, RqlActions.Select);
                return true;
            });

        var sut = CreateSut(RegisterResolver<ITestResolver>(resolverMock.Object));
        var root = Expression.Parameter(typeof(EntityWithJson), "e");

        // Act
        var result = sut.Build(root, "jsonProp.a.b.c");

        // Assert — error must mention the full path, not "jsonProp.a"
        result.IsError.Should().BeTrue();
        var error = result.Errors.Single();
        error.Message.Should().Contain("Filtering is not permitted");
        error.Code.Should().Contain("jsonProp.a.b.c");
    }

    [Fact]
    public void Build_NestedPath_ResolverRejectsDottedKey_ReturnsInvalidPropertyPathError()
    {
        // Resolvers that don't support nested paths simply return false for dotted keys;
        // the builder surfaces the standard "Invalid property path" error.

        // Arrange
        var resolverMock = new Mock<IRqlCustomPropertyResolver>();
        resolverMock
            .Setup(r => r.TryResolve(It.IsAny<Expression>(), It.IsAny<string>(), out It.Ref<Expression>.IsAny, out It.Ref<IRqlPropertyInfo>.IsAny))
            .Returns(false);

        var sut = CreateSut(RegisterResolver<ITestResolver>(resolverMock.Object));
        var root = Expression.Parameter(typeof(EntityWithJson), "e");

        // Act
        var result = sut.Build(root, "jsonProp.unknown.nested");

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Message.Contains("Invalid property path"));
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

        var sut = CreateSut(RegisterResolver<ITestResolver>(resolverMock.Object));
        var root = Expression.Parameter(typeof(EntityWithJson), "e");

        // Act
        var result = sut.Build(root, "jsonProp.dynamicProp");

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Message.Contains("Filtering is not permitted"));
    }

    [Fact]
    public void Build_StandardClrPath_WithResolverRegistered_DoesNotCallResolver()
    {
        // Arrange — resolver is registered for jsonProp but "name" is a standard CLR property
        var resolverMock = new Mock<IRqlCustomPropertyResolver>();

        var sut = CreateSut(RegisterResolver<ITestResolver>(resolverMock.Object));
        var root = Expression.Parameter(typeof(EntityWithJson), "e");

        // Act
        var result = sut.Build(root, "name");

        // Assert
        result.IsError.Should().BeFalse();
        result.Value!.PropertyInfo.Name.Should().Be("name");
        resolverMock.Verify(r => r.TryResolve(It.IsAny<Expression>(), It.IsAny<string>(), out It.Ref<Expression>.IsAny, out It.Ref<IRqlPropertyInfo>.IsAny), Times.Never);
    }

    [Fact]
    public void Build_UnknownRootSegment_DoesNotCallResolver_ReturnsError()
    {
        // Arrange — "unknownRoot" doesn't exist on the entity; no previous property with CustomResolver, so resolver is not called
        var resolverMock = new Mock<IRqlCustomPropertyResolver>();
        var sut = CreateSut(RegisterResolver<ITestResolver>(resolverMock.Object));
        var root = Expression.Parameter(typeof(EntityWithJson), "e");

        // Act
        var result = sut.Build(root, "unknownRoot");

        // Assert
        result.IsError.Should().BeTrue();
        resolverMock.Verify(r => r.TryResolve(It.IsAny<Expression>(), It.IsAny<string>(), out It.Ref<Expression>.IsAny, out It.Ref<IRqlPropertyInfo>.IsAny), Times.Never);
    }

    [Fact]
    public void Build_IgnoredCustomProperty_DoesNotFallThroughToResolver()
    {
        // An explicit `Mode = Ignored` on a CLR property must win over the parent's CustomResolver.
        // Otherwise a property hidden by design could be silently re-exposed via the resolver's
        // allowlist when the names happen to collide.

        // Arrange — resolver is wired, but the segment name matches an Ignored CLR property on the JsonElement-parent entity.
        var resolverMock = new Mock<IRqlCustomPropertyResolver>();
        resolverMock
            .Setup(r => r.TryResolve(It.IsAny<Expression>(), It.IsAny<string>(), out It.Ref<Expression>.IsAny, out It.Ref<IRqlPropertyInfo>.IsAny))
            .Returns((Expression parent, string name, out Expression expr, out IRqlPropertyInfo info) =>
            {
                expr = Expression.Constant("from-resolver", typeof(string));
                info = CreateSyntheticPropertyInfo(name);
                return true;
            });

        var sut = CreateSut(RegisterResolver<ITestResolver>(resolverMock.Object));
        var root = Expression.Parameter(typeof(EntityWithResolverCarrier), "e");

        // Act — "resolverCarrier.hiddenField": hiddenField exists on ResolverCarrier but is marked Ignored.
        var result = sut.Build(root, "resolverCarrier.hiddenField");

        // Assert — Ignored wins; no fallback to resolver, standard invalid-path error.
        result.IsError.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Message.Contains("Invalid property path"));
        resolverMock.Verify(
            r => r.TryResolve(It.IsAny<Expression>(), It.IsAny<string>(), out It.Ref<Expression>.IsAny, out It.Ref<IRqlPropertyInfo>.IsAny),
            Times.Never);
    }

    [Fact]
    public void MetadataFactory_CustomResolverTypeDoesNotImplementInterface_ThrowsInvalidCustomResolverException()
    {
        // Arrange — factory reads the attribute when building RqlPropertyInfo for a property
        var factory = new MetadataFactory(RqlSettingsFactory.Default());
        var property = typeof(EntityWithInvalidResolver).GetProperty(nameof(EntityWithInvalidResolver.Bad))!;

        // Act
        var act = () => factory.MakeRqlPropertyInfo("bad", property);

        // Assert
        act.Should().Throw<RqlInvalidCustomResolverException>()
            .WithMessage("*does not implement*IRqlCustomPropertyResolver*");
    }

    [Fact]
    public void Build_CustomResolverTypeNotRegistered_ThrowsInvalidCustomResolverException()
    {
        // Arrange — empty external services, so resolver type cannot be resolved
        var externalServices = new Mock<IExternalServiceAccessor>();
        externalServices.Setup(s => s.GetService(It.IsAny<Type>())).Returns((object?)null);

        var sut = CreateSut(externalServices);
        var root = Expression.Parameter(typeof(EntityWithJson), "e");

        // Act
        var act = () => sut.Build(root, "jsonProp.dynamicProp");

        // Assert
        act.Should().Throw<RqlInvalidCustomResolverException>()
            .WithMessage("*cannot be found*");
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

    // Marker interface representing a per-property resolver (mirrors how consumers wire this via DI).
    public interface ITestResolver : IRqlCustomPropertyResolver { }

    // Test entity:
    //   - JsonProp: has CustomResolver — children are resolved dynamically
    //   - PlainJson: no CustomResolver — children must fail
    //   - Name: standard primitive — resolver should never be touched
    public class EntityWithJson
    {
        [RqlProperty(CustomResolver = typeof(ITestResolver))]
        public JsonElement? JsonProp { get; set; }

        public JsonElement? PlainJson { get; set; }

        public string Name { get; set; } = null!;
    }

    // Entity exercising the Ignored + CustomResolver interaction.
    // `Carrier` carries the CustomResolver attribute; its child type exposes a CLR property
    // marked Ignored. An `Ignored` marker must win over the resolver fallback — the resolver
    // must NOT be invoked for that name.
    public class EntityWithResolverCarrier
    {
        [RqlProperty(CustomResolver = typeof(ITestResolver))]
        public ResolverCarrier ResolverCarrier { get; set; } = null!;
    }

    public class ResolverCarrier
    {
        [RqlProperty(Mode = RqlPropertyMode.Ignored)]
        public string HiddenField { get; set; } = null!;
    }

    public class EntityWithInvalidResolver
    {
        // `object` does not implement IRqlCustomPropertyResolver — must be rejected at metadata build time.
        [RqlProperty(CustomResolver = typeof(object))]
        public JsonElement? Bad { get; set; }
    }
}
