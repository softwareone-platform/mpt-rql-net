using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Mpt.Rql;
using Mpt.Rql.Core;
using Mpt.Rql.Core.Metadata;
using Mpt.Rql.Parsers.Linear.Services;
using Mpt.Rql.Services.Context;
using Mpt.Rql.Services.Filtering;
using Mpt.Rql.Settings;
using Rql.Tests.Unit.Services.Models;
using Xunit;

namespace Rql.Tests.Unit.Services;

/// <summary>
/// The expression stage compares against a right-hand PROPERTY when an unquoted constant resolves to one, so
/// the graph must include that column — but only for fully resolvable primitive paths, never for literals that
/// merely collide with a navigation name.
/// </summary>
public class FilteringGraphBuilderRightHandTests
{
    private readonly QueryContext<Product> _queryContext;
    private readonly FilteringGraphBuilder<Product> _filtering;
    private readonly RqlParser _parser = new();

    public FilteringGraphBuilderRightHandTests()
    {
        var validator = new Mock<IActionValidator>();
        validator.Setup(v => v.Validate(It.IsAny<RqlPropertyInfo>(), It.IsAny<RqlActions>())).Returns(true);

        var accessor = new ExternalServiceAccessor();
        accessor.SetServiceProvider(new ServiceCollection().BuildServiceProvider());
        _queryContext = new QueryContext<Product>(accessor);

        var metadata = new MetadataProvider(new PropertyNameProvider(), new MetadataFactory(new GlobalRqlSettings()));
        _filtering = new FilteringGraphBuilder<Product>(metadata, validator.Object, new BuilderContext());
    }

    private IRqlNode Traverse(string filter)
    {
        _filtering.TraverseRqlExpression(_queryContext.Graph, _parser.Parse(filter));
        return _queryContext.Graph;
    }

    [Fact]
    public void RightHandPrimitiveProperty_IsIncluded()
    {
        var root = Traverse("eq(description,name)");

        root.TryGetChild("description", out var left).Should().BeTrue();
        left!.IncludeReason.Should().HaveFlag(IncludeReasons.Filter);
        root.TryGetChild("name", out var right).Should().BeTrue();
        right!.IncludeReason.Should().HaveFlag(IncludeReasons.Filter);
    }

    [Fact]
    public void RightHandDottedPrimitivePath_IsIncluded()
    {
        var root = Traverse("eq(name,category.name)");

        root.TryGetChild("category", out var category).Should().BeTrue();
        category!.TryGetChild("name", out var name).Should().BeTrue();
        name!.IncludeReason.Should().HaveFlag(IncludeReasons.Filter);
    }

    [Fact]
    public void RightHandPrimitiveOfIncompatibleType_AddsNothing()
    {
        // The expression stage cannot coerce int `id` to string `name`, so it compares with the literal "id";
        // the graph must agree and not project a column the query never reads.
        var root = Traverse("eq(name,id)");

        root.Children.Should().ContainSingle(n => n.Name == "name");
    }

    [Fact]
    public void RightHandResolverBackedPath_IncludesTheCarrierProperty()
    {
        // `jsonProp.dynamicProp` has no CLR leaf; the expression stage hands it to the property's custom resolver,
        // so the graph must include the carrier `jsonProp` for mapping — just as it does when it is the left side.
        var validator = new Mock<IActionValidator>();
        validator.Setup(v => v.Validate(It.IsAny<RqlPropertyInfo>(), It.IsAny<RqlActions>())).Returns(true);
        var accessor = new ExternalServiceAccessor();
        accessor.SetServiceProvider(new ServiceCollection().BuildServiceProvider());
        var queryContext = new QueryContext<ResolverHolder>(accessor);
        var metadata = new MetadataProvider(new PropertyNameProvider(), new MetadataFactory(new GlobalRqlSettings()));
        var filtering = new FilteringGraphBuilder<ResolverHolder>(metadata, validator.Object, new BuilderContext());

        filtering.TraverseRqlExpression(queryContext.Graph, _parser.Parse("eq(name,jsonProp.dynamicProp)"));

        queryContext.Graph.TryGetChild("jsonProp", out var carrier).Should().BeTrue();
        carrier!.IncludeReason.Should().HaveFlag(IncludeReasons.Filter);
    }

    private sealed class ResolverHolder
    {
        public string Name { get; set; } = "";

        // The graph builder never invokes the resolver, so the interface type itself is a valid placeholder here.
        [RqlProperty(CustomResolver = typeof(Mpt.Rql.Abstractions.IRqlCustomPropertyResolver))]
        public string JsonProp { get; set; } = "";
    }

    [Theory]
    [InlineData("eq(name,category)")]        // reference navigation
    [InlineData("eq(name,items)")]           // collection navigation
    [InlineData("eq(name,items.name)")]      // path through a collection
    [InlineData("eq(name,nope.name)")]       // unresolvable prefix
    [InlineData("eq(name,ignoredCategory)")] // ignored property
    [InlineData("eq(name,'description')")]   // quoted literal
    [InlineData("eq(name,*)")]               // wildcard
    public void RightHandLiteralThatIsNotAPrimitivePath_AddsNothing(string filter)
    {
        var root = Traverse(filter);

        root.Children.Should().ContainSingle(n => n.Name == "name");
    }
}
