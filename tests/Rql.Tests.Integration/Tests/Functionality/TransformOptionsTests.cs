using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Mpt.Rql;
using Rql.Tests.Integration.Core;
using Rql.Tests.Integration.Tests.Functionality.ActionStrategy.Core;
using System.Globalization;
using System.Text.Json;
using Xunit;

namespace Rql.Tests.Integration.Tests.Functionality;

public class TransformOptionsTests
{
    private readonly IRqlQueryable<Product, ShapedProduct> _rql = RqlFactory.Make<Product, ShapedProduct>(services => { }, rql =>
    {
        rql.Settings.Select.Implicit = RqlSelectModes.Core | RqlSelectModes.Primitive | RqlSelectModes.Reference;
        rql.Settings.Select.Explicit = RqlSelectModes.All;
        rql.Settings.Select.MaxDepth = 10;
    });

    [Theory]
    [InlineData(null)]
    [InlineData("name")]
    public void Excluding_Property_LeavesItOutOfTheProjection(string? select)
    {
        // Act
        var result = _rql.Transform(ProductRepository.Query(), new RqlRequest { Select = select }, options => options.Override("name", include: false));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Query.ToList().Should().NotBeEmpty().And.OnlyContain(t => t.Name == null && t.Id > 0);
        var name = Child(result.Graph, "name");
        (name.IncludeReason, name.ExcludeReason).Should().Be((IncludeReasons.None, ExcludeReasons.Override));
        name.IsIncluded.Should().BeFalse();
    }

    [Fact]
    public void Excluding_NestedProperty_LeavesOnlyItOut()
    {
        // Act
        var result = _rql.Transform(ProductRepository.Query(), new RqlRequest(), options => options.Override("Reference.NAME", include: false));

        // Assert
        result.Query.ToList().Should().NotBeEmpty()
            .And.OnlyContain(t => t.Reference != null && t.Reference.Id == t.Id && t.Reference.Name == null && t.Name != null);
    }

    [Fact]
    public void Excluding_Property_StopsTheRequestSelectingBeneathIt()
    {
        // Act
        var result = _rql.Transform(ProductRepository.Query(), new RqlRequest { Select = "reference.name" }, options => options.Override("reference", include: false));

        // Assert
        result.Query.ToList().Should().NotBeEmpty().And.OnlyContain(t => t.Reference == null);
        Child(result.Graph, "reference").Count.Should().Be(0);
    }

    [Fact]
    public void Excluding_Property_DropsTheItemsOfItsGroup()
    {
        // Act — 'name' in the group belongs to the excluded reference, not to the root
        var result = _rql.Transform(ProductRepository.Query(), new RqlRequest { Select = "-name,reference(name)" }, options => options.Override("reference", include: false));

        // Assert
        result.Query.ToList().Should().NotBeEmpty().And.OnlyContain(t => t.Name == null && t.Reference == null);
    }

    [Theory]
    [InlineData("eq({0}.foo,bar)", null)]
    [InlineData(null, "+{0}.foo")]
    public void Excluding_Property_RejectsItAsAnActionStrategyAllowingNothingDoes(string? filter, string? order)
    {
        // Arrange — 'nothing' allows no action, and 'all' is of the same type
        var rql = MakeActionStrategyRql();
        RqlRequest Request(string property) => new()
        {
            Filter = filter == null ? null : string.Format(CultureInfo.InvariantCulture, filter, property),
            Order = order == null ? null : string.Format(CultureInfo.InvariantCulture, order, property),
        };
        var hidden = rql.Transform(ActionStrategyTestItemRepository.Query(), Request("nothing"));

        // Act
        var result = rql.Transform(ActionStrategyTestItemRepository.Query(), Request("all"), options => options.Override("all", include: false));

        // Assert
        hidden.IsSuccess.Should().BeFalse();
        result.IsSuccess.Should().BeFalse();
        result.Errors.Select(e => (e.Message, e.Path)).Should().Equal(hidden.Errors.Select(e => (e.Message, e.Path?.Replace("nothing", "all", StringComparison.Ordinal))));
        Child(result.Graph, "all").IncludeReason.Should().Be(Child(hidden.Graph, "nothing").IncludeReason);
    }

    [Theory]
    [InlineData("reference", "eq(reference.name,Jewelry Widget)", null, "Filtering is not permitted.")]
    [InlineData("reference.name", "eq(reference.name,Jewelry Widget)", null, "Filtering is not permitted.")]
    [InlineData("collection", "any(collection,eq(id,1))", null, "Filtering is not permitted.")]
    [InlineData("reference", null, "+reference.name", "Ordering is not permitted.")]
    [InlineData("collection.name", null, "+first(collection,eq(id,1)).name", "Ordering is not permitted.")]
    [InlineData("collection.id", null, "+first(collection,eq(id,1)).name", "Filtering is not permitted.")]
    public void Excluding_Property_RejectsFilteringAndOrderingByIt(string excluded, string? filter, string? order, string error)
    {
        // Act
        var result = _rql.Transform(ProductRepository.Query(), new RqlRequest { Filter = filter, Order = order }, options => options.Override(excluded, include: false));

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Errors.Should().ContainSingle().Which.Message.Should().Be(error);
    }

    [Fact]
    public void Including_PropertyThatMayNotBeSelected_BringsItIntoTheSelection()
    {
        // Arrange — 'nothing' may not be selected
        var rql = MakeActionStrategyRql();

        // Act
        var result = rql.Transform(ActionStrategyTestItemRepository.Query(), new RqlRequest(), options => options.Override("nothing", include: true));

        // Assert
        result.IsSuccess.Should().BeTrue();
        var nothing = Child(result.Graph, "nothing");
        (nothing.IncludeReason, nothing.ExcludeReason).Should().Be((IncludeReasons.Default | IncludeReasons.Override, ExcludeReasons.None));
        Child(nothing, "foo").IsIncluded.Should().BeTrue();
        result.Query.ToList().Should().NotBeEmpty().And.OnlyContain(t => t.Nothing != null);
    }

    [Fact]
    public void Including_PropertyTheSelectModeLeavesOut_BuildsWhatSelectingItBuilds()
    {
        // Arrange — 'hiddenCollection' is left out by its own select mode
        var rql = RqlFactory.Make<ShapedProduct>(services => { }, rql => rql.Settings.Select.Explicit = RqlSelectModes.All);
        var selected = rql.BuildGraph(new RqlRequest { Select = "hiddenCollection" });

        // Act
        var result = rql.Transform(ShapedProductRepository.Query(), new RqlRequest(), options => options.Override("hiddenCollection", include: true));

        // Assert
        result.IsSuccess.Should().BeTrue();
        Beneath(Child(result.Graph, "hiddenCollection")).Should().NotBeEmpty().And.Be(Beneath(Child(selected.Graph, "hiddenCollection")));
        result.Query.ToList().Should().NotBeEmpty()
            .And.OnlyContain(t => t.HiddenCollection.Count > 0 && t.HiddenCollection.All(c => c.Id > 0 && c.Name != null));
    }

    [Theory]
    [InlineData("name", "-name", null, null)]
    [InlineData("reference", "-reference", "eq(reference.id,1)", null)]
    [InlineData("reference", "-reference(-id)", null, null)]
    [InlineData("reference.name", "-reference", null, "+reference.id")]
    [InlineData("reference.name", "-reference,reference.id", null, null)]
    [InlineData("collection", "-collection", "any(collection,eq(id,1))", null)]
    [InlineData("collection.name", "-collection", null, "+first(collection,gt(id,0)).id")]
    public void Including_PropertyTheRequestDeselects_ChangesNothing(string included, string select, string? filter, string? order)
    {
        // Arrange — the request deselects the property, or the one above it
        var request = new RqlRequest { Select = select, Filter = filter, Order = order };
        var expected = JsonSerializer.Serialize(_rql.Transform(ProductRepository.Query(), request).Query.ToList());

        // Act
        var result = _rql.Transform(ProductRepository.Query(), request, options => options.Override(included, include: true));

        // Assert
        result.IsSuccess.Should().BeTrue();
        JsonSerializer.Serialize(result.Query.ToList()).Should().Be(expected);
    }

    [Fact]
    public void Including_PropertyBeneathOneLeftOut_LeavesItOut()
    {
        // Arrange — 'hiddenCollection' is left out by its own select mode
        var rql = RqlFactory.Make<ShapedProduct>(services => { }, rql => rql.Settings.Select.Explicit = RqlSelectModes.All);

        // Act
        var result = rql.Transform(ShapedProductRepository.Query(), new RqlRequest(), options => options.Override("hiddenCollection.name", include: true));

        // Assert
        Child(result.Graph, "hiddenCollection").IsIncluded.Should().BeFalse();
        result.Query.ToList().Should().NotBeEmpty().And.OnlyContain(t => t.HiddenCollection == null);
    }

    [Fact]
    public void Deciding_OnAPathThatIsNotAProperty_LeavesTheGraphAsIs()
    {
        // Arrange
        var expected = _rql.BuildGraph(new RqlRequest()).Graph.Print();

        // Act
        var result = _rql.BuildGraph(new RqlRequest(), options =>
        {
            options.Override("reference.unknown", include: false);
            options.Override("unknown.name", include: true);
        });

        // Assert
        result.Graph.Print().Should().Be(expected);
    }

    [Fact]
    public void LastDecisionOnAPath_Wins()
    {
        // Act
        var result = _rql.Transform(ProductRepository.Query(), new RqlRequest(), options =>
        {
            options.Override("name", include: false);
            options.Override("NAME", include: true);
        });

        // Assert
        result.Query.ToList().Should().NotBeEmpty().And.OnlyContain(t => t.Name != null);
    }

    [Fact]
    public void BuildGraph_TakesDecisionsIntoAccount()
    {
        // Act
        var response = _rql.BuildGraph(new RqlRequest(), options => options.Override("name", include: false));

        // Assert
        Child(response.Graph, "name").ExcludeReason.Should().Be(ExcludeReasons.Override);
    }

    [Fact]
    public void Decisions_ApplyToTheirCallOnly()
    {
        // Act
        var configured = _rql.Transform(ProductRepository.Query(), new RqlRequest(), options => options.Override("name", include: false));
        var unconfigured = _rql.Transform(ProductRepository.Query(), new RqlRequest());

        // Assert
        configured.Query.ToList().Should().OnlyContain(t => t.Name == null);
        unconfigured.Query.ToList().Should().OnlyContain(t => t.Name != null);
    }

    [Fact]
    public void Settings_ApplyToTheirCallOnly()
    {
        // Act
        var configured = _rql.Transform(ProductRepository.Query(), new RqlRequest(), options => options.Settings.Select.Explicit = RqlSelectModes.None);
        var unconfigured = _rql.Transform(ProductRepository.Query(), new RqlRequest());

        // Assert
        configured.Query.ToList().Should().OnlyContain(t => t.Name == null);
        unconfigured.Query.ToList().Should().OnlyContain(t => t.Name != null);
    }

    private static IRqlQueryable<ActionStrategyTestItem, ActionStrategyTestItem> MakeActionStrategyRql()
        => RqlFactory.Make<ActionStrategyTestItem>(services =>
        {
            services.AddScoped<AllowAllActionStrategy>();
            services.AddScoped<AllowNothingActionStrategy>();
            services.AddScoped<AllowOnlyFilterActionStrategy>();
            services.AddScoped<AllowOnlyOrderActionStrategy>();
            services.AddScoped<AllowOnlySelectActionStrategy>();
        }, rql =>
        {
            rql.Settings.Select.Implicit = RqlSelectModes.Core | RqlSelectModes.Primitive | RqlSelectModes.Reference;
            rql.Settings.Select.Explicit = RqlSelectModes.All;
        });

    private static IRqlNode Child(IRqlNode node, string name)
    {
        node.TryGetChild(name, out var child).Should().BeTrue($"'{name}' is in the graph");
        return child!;
    }

    private static string Beneath(IRqlNode node) => string.Concat(node.Children.OrderBy(t => t.Name).Select(t => t.Print()));
}
