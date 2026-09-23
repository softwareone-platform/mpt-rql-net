using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Mpt.Rql;
using Mpt.Rql.Core;
using Mpt.Rql.Core.Metadata;
using Mpt.Rql.Parsers.Linear.Services;
using Mpt.Rql.Services.Context;
using Mpt.Rql.Services.Filtering;
using Mpt.Rql.Services.Ordering;
using Mpt.Rql.Services.Ordering.Functions;
using Mpt.Rql.Settings;
using Rql.Tests.Unit.Services.Models;
using Xunit;

namespace Rql.Tests.Unit.Ordering;

public class OrderingGraphBuilderFunctionTests
{
    private readonly QueryContext<Product> _queryContext;
    private readonly OrderingGraphBuilder<Product> _ordering;
    private readonly RqlParser _parser = new();

    public OrderingGraphBuilderFunctionTests()
    {
        var validator = new Mock<IActionValidator>();
        validator.Setup(v => v.Validate(It.IsAny<RqlPropertyInfo>(), It.IsAny<RqlActions>())).Returns(true);

        var accessor = new ExternalServiceAccessor();
        accessor.SetServiceProvider(new ServiceCollection().BuildServiceProvider());
        _queryContext = new QueryContext<Product>(accessor);

        var metadata = new MetadataProvider(new PropertyNameProvider(), new MetadataFactory(new GlobalRqlSettings()));
        var builderContext = new BuilderContext();
        var filtering = new FilteringGraphBuilder<Product>(metadata, validator.Object, builderContext);
        var registry = new OrderingFunctionRegistry([new FirstOrderingFunction()]);

        _ordering = new OrderingGraphBuilder<Product>(metadata, validator.Object, builderContext, filtering, registry);
    }

    private RqlNode Traverse(string order)
    {
        _ordering.TraverseRqlExpression(_queryContext.Graph, _parser.Parse(order));
        return _queryContext.Graph;
    }

    private static IRqlNode Child(IRqlNode node, string name)
    {
        node.TryGetChild(name, out var child).Should().BeTrue($"expected child '{name}' under '{node.GetFullPath()}'");
        return child!;
    }

    [Fact]
    public void First_WithPredicate_IncludesCollectionPredicateAndSelector_UnderTheCollection()
    {
        var root = Traverse("+first(items,eq(name,x)).description");

        var items = Child(root, "items");
        items.IncludeReason.Should().HaveFlag(IncludeReasons.Hierarchy);

        Child(items, "name").IncludeReason.Should().HaveFlag(IncludeReasons.Filter);
        Child(items, "description").IncludeReason.Should().HaveFlag(IncludeReasons.Order);
    }

    [Fact]
    public void First_DoesNotResolveArgumentsAgainstTheRoot()
    {
        var root = Traverse("+first(items,eq(name,x)).description");

        root.TryGetChild("name", out _).Should().BeFalse();
        root.TryGetChild("description", out _).Should().BeFalse();
        root.TryGetChild("first", out _).Should().BeFalse();
    }

    [Fact]
    public void First_TwoArguments_IncludesCollectionAndSelector()
    {
        var root = Traverse("-first(items).description");

        var items = Child(root, "items");
        items.IncludeReason.Should().HaveFlag(IncludeReasons.Hierarchy);
        Child(items, "description").IncludeReason.Should().HaveFlag(IncludeReasons.Order);
        items.TryGetChild("name", out _).Should().BeFalse();
    }

    [Fact]
    public void First_DottedCollectionPath_IncludesEverySegmentAsHierarchy()
    {
        var root = Traverse("+first(category.products,eq(name,x)).description");

        var category = Child(root, "category");
        category.IncludeReason.Should().HaveFlag(IncludeReasons.Hierarchy);
        var products = Child(category, "products");
        products.IncludeReason.Should().HaveFlag(IncludeReasons.Hierarchy);
        Child(products, "name").IncludeReason.Should().HaveFlag(IncludeReasons.Filter);
        Child(products, "description").IncludeReason.Should().HaveFlag(IncludeReasons.Order);
    }

    [Fact]
    public void First_CombinedWithScalarItem_IncludesBoth()
    {
        var root = Traverse("+first(items,eq(name,x)).description,-id");

        Child(root, "id").IncludeReason.Should().HaveFlag(IncludeReasons.Order);
        Child(Child(root, "items"), "description").IncludeReason.Should().HaveFlag(IncludeReasons.Order);
    }

    [Fact]
    public void UnknownFunction_DoesNotMutateTheGraph()
    {
        var root = Traverse("+nope(items,description)");

        root.Count.Should().Be(0);
    }

    [Theory]
    [InlineData("+first(*).id")]
    [InlineData("+first(+*).id")]
    [InlineData("+first(items).-*")]
    public void First_WildcardArgument_DoesNotMutateTheGraph(string order)
    {
        // Build rejects wildcards; the graph must not have fanned out every property in the meantime.
        var root = Traverse(order);

        root.Count.Should().Be(0);
    }

    [Fact]
    public void First_UnknownCollection_DoesNotMutateTheGraph()
    {
        var root = Traverse("+first(nope,eq(name,x)).description");

        root.Count.Should().Be(0);
    }

    [Fact]
    public void PlainOrderItems_StillWork()
    {
        var root = Traverse("+name,-id");

        Child(root, "name").IncludeReason.Should().HaveFlag(IncludeReasons.Order);
        Child(root, "id").IncludeReason.Should().HaveFlag(IncludeReasons.Order);
    }

    [Fact]
    public void SignOnlyGroup_IsTreatedAsPlainOrderItems()
    {
        var root = Traverse("+(name,id)");

        Child(root, "name").IncludeReason.Should().HaveFlag(IncludeReasons.Order);
        Child(root, "id").IncludeReason.Should().HaveFlag(IncludeReasons.Order);
    }

    [Fact]
    public void PredicateRightHandProperty_IsIncludedUnderTheCollection()
    {
        // eq(name,description) compares two element properties; both columns must be projected
        var root = Traverse("+first(items,eq(name,description)).id");

        var items = Child(root, "items");
        Child(items, "name").IncludeReason.Should().HaveFlag(IncludeReasons.Filter);
        Child(items, "description").IncludeReason.Should().HaveFlag(IncludeReasons.Filter);
        Child(items, "id").IncludeReason.Should().HaveFlag(IncludeReasons.Order);
    }
}
