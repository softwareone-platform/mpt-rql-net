using FluentAssertions;
using Moq;
using Mpt.Rql;
using Mpt.Rql.Abstractions;
using Mpt.Rql.Core;
using Mpt.Rql.Services.Context;
using Mpt.Rql.Services.Ordering;
using Rql.Tests.Common.Factory;
using System.Linq.Expressions;
using System.Text.Json;
using Xunit;

namespace Rql.Tests.Unit.Ordering;

public class CustomPropertyResolverOrderingTests
{
    private static OrderingPathInfoBuilder CreateSut(Mock<IExternalServiceAccessor> externalServicesMock)
    {
        return new OrderingPathInfoBuilder(
            new SimpleActionValidator(),
            MetadataProviderFactory.Internal(),
            new BuilderContext(),
            RqlSettingsFactory.Default(),
            externalServicesMock.Object);
    }

    private static Mock<IExternalServiceAccessor> RegisterResolver(IRqlCustomPropertyResolver resolver)
    {
        var mock = new Mock<IExternalServiceAccessor>();
        mock.Setup(s => s.GetService(typeof(ITestResolver))).Returns(resolver);
        return mock;
    }

    [Fact]
    public void Build_OrderByResolverPath_SyntheticPermitsOrder_ReturnsResolverExpression()
    {
        // Arrange — resolver returns a property that allows Order
        var leaf = Expression.Constant("ordered", typeof(string));
        var resolverMock = new Mock<IRqlCustomPropertyResolver>();
        resolverMock
            .Setup(r => r.TryResolve(It.IsAny<Expression>(), "dynamicProp", out It.Ref<Expression>.IsAny, out It.Ref<IRqlPropertyInfo>.IsAny))
            .Returns((Expression parent, string name, out Expression expr, out IRqlPropertyInfo info) =>
            {
                expr = leaf;
                info = CreateSyntheticPropertyInfo(name, RqlActions.Order);
                return true;
            });

        var sut = CreateSut(RegisterResolver(resolverMock.Object));
        var root = Expression.Parameter(typeof(EntityWithJson), "e");

        // Act
        var result = sut.Build(root, "jsonProp.dynamicProp");

        // Assert — same dispatch as filter, but validates the Order action instead
        result.IsError.Should().BeFalse();
        result.Value!.Expression.Should().BeSameAs(leaf);
        result.Value.PropertyInfo.Name.Should().Be("dynamicProp");
    }

    [Fact]
    public void Build_OrderByResolverPath_SyntheticDoesNotPermitOrder_ReturnsOrderingNotPermittedError()
    {
        // Arrange — resolver returns a Filter-only property (no Order action)
        var resolverMock = new Mock<IRqlCustomPropertyResolver>();
        resolverMock
            .Setup(r => r.TryResolve(It.IsAny<Expression>(), "dynamicProp", out It.Ref<Expression>.IsAny, out It.Ref<IRqlPropertyInfo>.IsAny))
            .Returns((Expression parent, string name, out Expression expr, out IRqlPropertyInfo info) =>
            {
                expr = Expression.Constant("resolved", typeof(string));
                info = CreateSyntheticPropertyInfo(name, RqlActions.Filter);
                return true;
            });

        var sut = CreateSut(RegisterResolver(resolverMock.Object));
        var root = Expression.Parameter(typeof(EntityWithJson), "e");

        // Act
        var result = sut.Build(root, "jsonProp.dynamicProp");

        // Assert — validation surfaces the ordering-specific error message
        result.IsError.Should().BeTrue();
        result.Errors.Should().Contain(e => e.Message.Contains("Ordering is not permitted"));
    }

    [Fact]
    public void Build_OrderByNestedResolverPath_ForwardsFullDottedKeyToResolver()
    {
        // Arrange — exactly like the filter nested test, but exercised through the ordering builder
        var leaf = Expression.Constant("ordered", typeof(string));
        var resolverMock = new Mock<IRqlCustomPropertyResolver>();
        resolverMock
            .Setup(r => r.TryResolve(It.IsAny<Expression>(), "a.b.c", out It.Ref<Expression>.IsAny, out It.Ref<IRqlPropertyInfo>.IsAny))
            .Returns((Expression parent, string name, out Expression expr, out IRqlPropertyInfo info) =>
            {
                expr = leaf;
                info = CreateSyntheticPropertyInfo(name, RqlActions.Order);
                return true;
            });

        var sut = CreateSut(RegisterResolver(resolverMock.Object));
        var root = Expression.Parameter(typeof(EntityWithJson), "e");

        // Act
        var result = sut.Build(root, "jsonProp.a.b.c");

        // Assert
        result.IsError.Should().BeFalse();
        result.Value!.PropertyInfo.Name.Should().Be("a.b.c");
        result.Value.Expression.Should().BeSameAs(leaf);
    }

    private static IRqlPropertyInfo CreateSyntheticPropertyInfo(string name, RqlActions actions)
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

    public interface ITestResolver : IRqlCustomPropertyResolver { }

    public class EntityWithJson
    {
        [RqlProperty(Actions = RqlActions.Order, CustomResolver = typeof(ITestResolver))]
        public JsonElement? JsonProp { get; set; }
    }
}
