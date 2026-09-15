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
