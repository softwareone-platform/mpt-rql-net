using FluentAssertions;
using Mpt.Rql;
using Mpt.Rql.Core.Metadata;
using Mpt.Rql.Services.Context;
using Mpt.Rql.Settings;
using Rql.Tests.Unit.Services.Models;
using Xunit;

namespace Rql.Tests.Unit.Services;

public class BuilderContextTests
{
    private static (BuilderContext context, RqlNode root) MakeGraph()
    {
        IMetadataProvider metadata = new MetadataProvider(new PropertyNameProvider(), new MetadataFactory(new GlobalRqlSettings()));
        var root = RqlNode.MakeRoot();
        metadata.TryGetPropertyByDisplayName(typeof(Product), "category", out var category);
        metadata.TryGetPropertyByDisplayName(typeof(Category), "products", out var products);
        var categoryNode = root.IncludeChild(category!, IncludeReasons.Hierarchy);
        categoryNode.IncludeChild(products!, IncludeReasons.Hierarchy);

        var context = new BuilderContext();
        context.SetNode(root);
        return (context, root);
    }

    [Fact]
    public void TryGoToChild_ByName_DescendsAndPrefixesPaths()
    {
        var (context, _) = MakeGraph();

        context.TryGoToChild("category").Should().BeTrue();
        context.TryGoToChild("products").Should().BeTrue();

        context.GetFullPath("id").Should().Be("category.products.id");
    }

    [Fact]
    public void TryGoToChild_ByName_UnknownChild_ReturnsFalseAndStays()
    {
        var (context, root) = MakeGraph();

        context.TryGoToChild("nope").Should().BeFalse();

        context.CurrentNode.Should().BeSameAs(root);
    }

    [Fact]
    public void TryGoToChild_ByName_WithoutCurrentNode_ReturnsFalse()
    {
        var context = new BuilderContext();

        context.TryGoToChild("category").Should().BeFalse();
    }
}
