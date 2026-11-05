using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Mpt.Rql;
using Mpt.Rql.Abstractions;
using Mpt.Rql.Core;
using Mpt.Rql.Core.Metadata;
using Mpt.Rql.Parsers.Linear.Services;
using Mpt.Rql.Services.Context;
using Mpt.Rql.Services.Projection;
using Mpt.Rql.Settings;
using System.Collections;
using Xunit;

namespace Rql.Tests.Unit.Services;

public class ProjectionGraphBuilderDependencyTests
{
    // a cycle test runs the build on a worker so that xUnit fails it at this timeout rather than letting it hang,
    // as xUnit enforces Timeout only once the test method yields
    private const int CycleTimeoutMilliseconds = 5000;

    private readonly ProjectionGraphBuilder<View> _projectionBuilder;
    private readonly IQueryContext<View> _queryContext;
    private readonly RqlParser _rqlParser;

    public ProjectionGraphBuilderDependencyTests()
    {
        var services = new ServiceCollection();
        var serviceAccessor = new ExternalServiceAccessor();
        serviceAccessor.SetServiceProvider(services.BuildServiceProvider());

        _queryContext = new QueryContext<View>(serviceAccessor);
        _rqlParser = new RqlParser();

        var settings = new GlobalRqlSettings();
        settings.Select.Implicit = RqlSelectModes.Core | RqlSelectModes.Primitive;
        settings.Select.Explicit = RqlSelectModes.All;

        var metadataProvider = new MetadataProvider(new PropertyNameProvider(), new MetadataFactory(settings));
        var builderContext = new BuilderContext();

        // the real validator, as the fixture declares properties with Actions = None
        var actionValidator = new ActionValidator(serviceAccessor);

        _projectionBuilder = new ProjectionGraphBuilder<View>(_queryContext, metadataProvider, actionValidator, builderContext, settings, new RqlTransformOptions(settings));
    }

    [Fact]
    public void BuildDefaults_WhenComputedPropertySelected_IncludesNonSelectableDependenciesForDependencyAlone()
    {
        // Act
        Build("value");

        // Assert
        var value = GetNode(_queryContext.Graph, "value");
        value.IncludeReason.Should().HaveFlag(IncludeReasons.Select);
        value.IsIncluded.Should().BeTrue();

        // raw and format are dependencies of value that declare Actions = None
        var raw = GetNode(_queryContext.Graph, "raw");
        (raw.IncludeReason, raw.ExcludeReason).Should().Be((IncludeReasons.Dependency, ExcludeReasons.Invisible));
        raw.IsIncluded.Should().BeTrue();

        var format = GetNode(_queryContext.Graph, "format");
        (format.IncludeReason, format.ExcludeReason).Should().Be((IncludeReasons.Dependency, ExcludeReasons.Invisible));
        format.IsIncluded.Should().BeTrue();
    }

    [Fact]
    public void BuildDefaults_WhenComputedPropertyNotProjected_DoesNotIncludeDependencies()
    {
        // Act
        Build("-*,id");

        // Assert
        GetNode(_queryContext.Graph, "value").IsIncluded.Should().BeFalse();

        var raw = GetNode(_queryContext.Graph, "raw");
        (raw.IncludeReason, raw.ExcludeReason).Should().Be((IncludeReasons.None, ExcludeReasons.Invisible));
        raw.IsIncluded.Should().BeFalse();

        var format = GetNode(_queryContext.Graph, "format");
        (format.IncludeReason, format.ExcludeReason).Should().Be((IncludeReasons.None, ExcludeReasons.Invisible));
        format.IsIncluded.Should().BeFalse();
    }

    [Fact]
    public void BuildDefaults_WhenOnlySiblingOfNestedComputedPropertySelected_DoesNotIncludeDependencies()
    {
        // Act
        Build("-*,parameter.externalId");

        // Assert
        var parameter = GetNode(_queryContext.Graph, "parameter");
        parameter.IsIncluded.Should().BeTrue();
        GetNode(parameter, "externalId").IsIncluded.Should().BeTrue();
        GetNode(parameter, "value").IsIncluded.Should().BeFalse();

        var raw = GetNode(parameter, "raw");
        raw.IncludeReason.Should().NotHaveFlag(IncludeReasons.Dependency);
        raw.IsIncluded.Should().BeFalse();

        var format = GetNode(parameter, "format");
        format.IncludeReason.Should().NotHaveFlag(IncludeReasons.Dependency);
        format.IsIncluded.Should().BeFalse();
    }

    [Fact]
    public void BuildDefaults_WhenParentOfComputedPropertyDeselected_DoesNotIncludeDependencies()
    {
        // Act
        Build("-parameter");

        // Assert
        var parameter = GetNode(_queryContext.Graph, "parameter");
        parameter.IsIncluded.Should().BeFalse();

        var raw = GetNode(parameter, "raw");
        raw.IncludeReason.Should().NotHaveFlag(IncludeReasons.Dependency);
        raw.IsIncluded.Should().BeFalse();

        var format = GetNode(parameter, "format");
        format.IncludeReason.Should().NotHaveFlag(IncludeReasons.Dependency);
        format.IsIncluded.Should().BeFalse();
    }

    [Fact]
    public void BuildDefaults_WhenSelectableDependencyDeselected_IncludesItKeepingUnselected()
    {
        // Act
        Build("label,-name");

        // Assert
        GetNode(_queryContext.Graph, "label").IsIncluded.Should().BeTrue();

        var name = GetNode(_queryContext.Graph, "name");
        (name.IncludeReason, name.ExcludeReason).Should().Be((IncludeReasons.Default | IncludeReasons.Dependency, ExcludeReasons.Unselected));
        name.IsIncluded.Should().BeTrue();
    }

    [Fact]
    public void BuildDefaults_WhenNonSelectableDependencyDeselected_IncludesItAsInvisible()
    {
        // Act
        Build("value,-format");

        // Assert
        // format is a dependency of value that declares Actions = None
        var format = GetNode(_queryContext.Graph, "format");
        (format.IncludeReason, format.ExcludeReason).Should().Be((IncludeReasons.Dependency, ExcludeReasons.Invisible));
        format.IsIncluded.Should().BeTrue();
    }

    [Fact]
    public void BuildDefaults_WhenDependencyDeclaresDependencies_IncludesThemTransitively()
    {
        // Act
        Build("-*,total");

        // Assert
        GetNode(_queryContext.Graph, "total").IsIncluded.Should().BeTrue();

        var subtotal = GetNode(_queryContext.Graph, "subtotal");
        (subtotal.IncludeReason, subtotal.ExcludeReason).Should().Be((IncludeReasons.Default | IncludeReasons.Dependency, ExcludeReasons.Unselected));
        subtotal.IsIncluded.Should().BeTrue();

        // total declares only subtotal, so amount comes in through subtotal's own declaration
        var amount = GetNode(_queryContext.Graph, "amount");
        (amount.IncludeReason, amount.ExcludeReason).Should().Be((IncludeReasons.Default | IncludeReasons.Dependency, ExcludeReasons.Unselected));
        amount.IsIncluded.Should().BeTrue();
    }

    [Fact]
    public void BuildDefaults_WhenDependencySelected_DoesNotIncludeTheDependentProperty()
    {
        // Act
        Build("-*,subtotal");

        // Assert
        GetNode(_queryContext.Graph, "amount").IncludeReason.Should().HaveFlag(IncludeReasons.Dependency);

        var total = GetNode(_queryContext.Graph, "total");
        total.IncludeReason.Should().NotHaveFlag(IncludeReasons.Dependency);
        total.IsIncluded.Should().BeFalse();
    }

    [Fact(Timeout = CycleTimeoutMilliseconds)]
    public async Task BuildDefaults_WhenDependenciesFormCycle_TerminatesAndIncludesBoth()
    {
        // Act
        var graph = await Task.Run(() => BuildGraph<TwoNodeCycleView>("-*,cycleA"));

        // Assert
        var cycleA = GetNode(graph, "cycleA");
        cycleA.IncludeReason.Should().HaveFlag(IncludeReasons.Select);
        cycleA.IsIncluded.Should().BeTrue();

        var cycleB = GetNode(graph, "cycleB");
        (cycleB.IncludeReason, cycleB.ExcludeReason).Should().Be((IncludeReasons.Default | IncludeReasons.Dependency, ExcludeReasons.Unselected));
        cycleB.IsIncluded.Should().BeTrue();
    }

    [Fact(Timeout = CycleTimeoutMilliseconds)]
    public async Task BuildDefaults_WhenDependenciesFormThreeNodeCycle_TerminatesAndIncludesAll()
    {
        // Act
        var graph = await Task.Run(() => BuildGraph<ThreeNodeCycleView>("-*,a"));

        // Assert
        var a = GetNode(graph, "a");
        a.IncludeReason.Should().HaveFlag(IncludeReasons.Select);
        a.IsIncluded.Should().BeTrue();

        // a declares b, b declares c, and c declares a
        var b = GetNode(graph, "b");
        (b.IncludeReason, b.ExcludeReason).Should().Be((IncludeReasons.Default | IncludeReasons.Dependency, ExcludeReasons.Unselected));
        b.IsIncluded.Should().BeTrue();

        var c = GetNode(graph, "c");
        (c.IncludeReason, c.ExcludeReason).Should().Be((IncludeReasons.Default | IncludeReasons.Dependency, ExcludeReasons.Unselected));
        c.IsIncluded.Should().BeTrue();
    }

    [Fact(Timeout = CycleTimeoutMilliseconds)]
    public async Task BuildDefaults_WhenDependencyCycleHasBranch_TerminatesAndIncludesBranch()
    {
        // Act
        var graph = await Task.Run(() => BuildGraph<BranchedCycleView>("-*,a"));

        // Assert
        var a = GetNode(graph, "a");
        a.IncludeReason.Should().HaveFlag(IncludeReasons.Select);
        a.IsIncluded.Should().BeTrue();

        // a declares b and c, b declares a back, and c declares d, which declares nothing
        var b = GetNode(graph, "b");
        (b.IncludeReason, b.ExcludeReason).Should().Be((IncludeReasons.Default | IncludeReasons.Dependency, ExcludeReasons.Unselected));
        b.IsIncluded.Should().BeTrue();

        var c = GetNode(graph, "c");
        (c.IncludeReason, c.ExcludeReason).Should().Be((IncludeReasons.Default | IncludeReasons.Dependency, ExcludeReasons.Unselected));
        c.IsIncluded.Should().BeTrue();

        var d = GetNode(graph, "d");
        (d.IncludeReason, d.ExcludeReason).Should().Be((IncludeReasons.Default | IncludeReasons.Dependency, ExcludeReasons.Unselected));
        d.IsIncluded.Should().BeTrue();
    }

    [Fact]
    public void BuildDefaults_WhenComputedPropertyIncludedByDefaultsOfSelectedParent_IncludesDependencies()
    {
        // Act
        Build("-*,parameters");

        // Assert
        var parameters = GetNode(_queryContext.Graph, "parameters");
        parameters.IncludeReason.Should().HaveFlag(IncludeReasons.Select);

        var value = GetNode(parameters, "value");
        value.IncludeReason.Should().HaveFlag(IncludeReasons.Default);
        value.IncludeReason.Should().NotHaveFlag(IncludeReasons.Select);
        value.IsIncluded.Should().BeTrue();

        var raw = GetNode(parameters, "raw");
        (raw.IncludeReason, raw.ExcludeReason).Should().Be((IncludeReasons.Dependency, ExcludeReasons.Invisible));
        raw.IsIncluded.Should().BeTrue();

        var format = GetNode(parameters, "format");
        (format.IncludeReason, format.ExcludeReason).Should().Be((IncludeReasons.Dependency, ExcludeReasons.Invisible));
        format.IsIncluded.Should().BeTrue();
    }

    [Fact]
    public void BuildDefaults_WhenNoSelect_IncludesDependenciesOfImplicitlyProjectedReference()
    {
        // Act
        Build(string.Empty);

        // Assert
        var parameter = GetNode(_queryContext.Graph, "parameter");
        parameter.IncludeReason.Should().Be(IncludeReasons.Default);

        GetNode(parameter, "value").IsIncluded.Should().BeTrue();

        var raw = GetNode(parameter, "raw");
        (raw.IncludeReason, raw.ExcludeReason).Should().Be((IncludeReasons.Dependency, ExcludeReasons.Invisible));
        raw.IsIncluded.Should().BeTrue();
    }

    [Fact]
    public void BuildDefaults_WhenComputedPropertyOfCollectionElementSelected_IncludesDependenciesUnderCollection()
    {
        // Act
        Build("-*,parameters.value");

        // Assert
        var parameters = GetNode(_queryContext.Graph, "parameters");
        parameters.IsIncluded.Should().BeTrue();
        GetNode(parameters, "value").IncludeReason.Should().HaveFlag(IncludeReasons.Select);

        var raw = GetNode(parameters, "raw");
        (raw.IncludeReason, raw.ExcludeReason).Should().Be((IncludeReasons.Dependency, ExcludeReasons.Invisible));
        raw.IsIncluded.Should().BeTrue();

        var format = GetNode(parameters, "format");
        (format.IncludeReason, format.ExcludeReason).Should().Be((IncludeReasons.Dependency, ExcludeReasons.Invisible));
        format.IsIncluded.Should().BeTrue();

        // the root's own raw stays out, as the request deselects the root value that declares it
        var rootRaw = GetNode(_queryContext.Graph, "raw");
        rootRaw.IncludeReason.Should().NotHaveFlag(IncludeReasons.Dependency);
        rootRaw.IsIncluded.Should().BeFalse();
    }

    [Fact]
    public void BuildDefaults_WhenComputedPropertyUnderReferenceImplementingEnumerableSelected_IncludesDependencies()
    {
        // Act
        var graph = BuildGraph<ContainerView>("-*,bag.value");

        // Assert
        var bag = GetNode(graph, "bag");
        bag.IsIncluded.Should().BeTrue();
        GetNode(bag, "value").IsIncluded.Should().BeTrue();

        // raw is the dependency of Bag.Value that declares Actions = None
        var raw = GetNode(bag, "raw");
        (raw.IncludeReason, raw.ExcludeReason).Should().Be((IncludeReasons.Dependency, ExcludeReasons.Invisible));
        raw.IsIncluded.Should().BeTrue();
    }

    [Fact]
    public void BuildDefaults_WhenComputedPropertyOfArrayElementSelected_IncludesDependenciesUnderArray()
    {
        // Act
        var graph = BuildGraph<ContainerView>("-*,items.value");

        // Assert
        var items = GetNode(graph, "items");
        items.IsIncluded.Should().BeTrue();
        GetNode(items, "value").IncludeReason.Should().HaveFlag(IncludeReasons.Select);

        // raw is the dependency of Item.Value that declares Actions = None
        var raw = GetNode(items, "raw");
        raw.IncludeReason.Should().HaveFlag(IncludeReasons.Dependency);
        raw.IsIncluded.Should().BeTrue();
    }

    [Fact]
    public void BuildDefaults_WhenInheritedComputedPropertySelected_IncludesInheritedDependencies()
    {
        // Act
        var graph = BuildGraph<ContainerView>("-*,child.value");

        // Assert
        var child = GetNode(graph, "child");
        child.IsIncluded.Should().BeTrue();
        GetNode(child, "value").IncludeReason.Should().HaveFlag(IncludeReasons.Select);

        // value and its dependency raw, which declares Actions = None, are both declared on BaseItem
        // and reached through DerivedItem
        var raw = GetNode(child, "raw");
        (raw.IncludeReason, raw.ExcludeReason).Should().Be((IncludeReasons.Dependency, ExcludeReasons.Invisible));
        raw.IsIncluded.Should().BeTrue();
    }

    private void Build(string select)
    {
        _projectionBuilder.BuildDecisions();
        _projectionBuilder.TraverseRqlExpression(_queryContext.Graph, _rqlParser.Parse(select));
        _projectionBuilder.BuildDefaults();
    }

    // the same setup as the constructor, for a root view other than View
    private static IRqlNode BuildGraph<TView>(string select)
    {
        var services = new ServiceCollection();
        var serviceAccessor = new ExternalServiceAccessor();
        serviceAccessor.SetServiceProvider(services.BuildServiceProvider());

        var queryContext = new QueryContext<TView>(serviceAccessor);

        var settings = new GlobalRqlSettings();
        settings.Select.Implicit = RqlSelectModes.Core | RqlSelectModes.Primitive;
        settings.Select.Explicit = RqlSelectModes.All;

        var metadataProvider = new MetadataProvider(new PropertyNameProvider(), new MetadataFactory(settings));
        var projectionBuilder = new ProjectionGraphBuilder<TView>(queryContext, metadataProvider, new ActionValidator(serviceAccessor), new BuilderContext(), settings, new RqlTransformOptions(settings));

        projectionBuilder.BuildDecisions();
        projectionBuilder.TraverseRqlExpression(queryContext.Graph, new RqlParser().Parse(select));
        projectionBuilder.BuildDefaults();

        return queryContext.Graph;
    }

    private static IRqlNode GetNode(IRqlNode parent, string name)
    {
        parent.TryGetChild(name, out var child).Should().BeTrue($"node '{name}' should exist under '{parent.Name}'");
        return child!;
    }

    private class View
    {
        public string Id { get; set; } = null!;

        public string ExternalId { get; set; } = null!;

        [RqlProperty(DependsOn = new[] { nameof(Raw), nameof(Format) })]
        public string Value => string.Format(Format, Raw);

        [RqlProperty(Actions = RqlActions.None)]
        public string Raw { get; set; } = null!;

        [RqlProperty(Actions = RqlActions.None)]
        public string Format { get; set; } = null!;

        public string Name { get; set; } = null!;

        [RqlProperty(DependsOn = new[] { nameof(Name) })]
        public string Label => Name;

        [RqlProperty(DependsOn = new[] { nameof(Subtotal) })]
        public decimal Total => Subtotal;

        [RqlProperty(DependsOn = new[] { nameof(Amount) })]
        public decimal Subtotal => Amount;

        public decimal Amount { get; set; }

        public Parameter Parameter { get; set; } = null!;

        public List<Parameter> Parameters { get; set; } = null!;
    }

    private class Parameter
    {
        public string Id { get; set; } = null!;

        public string ExternalId { get; set; } = null!;

        [RqlProperty(DependsOn = new[] { nameof(Raw), nameof(Format) })]
        public string Value => string.Format(Format, Raw);

        [RqlProperty(Actions = RqlActions.None)]
        public string Raw { get; set; } = null!;

        [RqlProperty(Actions = RqlActions.None)]
        public string Format { get; set; } = null!;
    }

    private class TwoNodeCycleView
    {
        [RqlProperty(DependsOn = new[] { nameof(CycleB) })]
        public string CycleA { get; set; } = null!;

        [RqlProperty(DependsOn = new[] { nameof(CycleA) })]
        public string CycleB { get; set; } = null!;
    }

    private class ThreeNodeCycleView
    {
        [RqlProperty(DependsOn = new[] { nameof(B) })]
        public string A { get; set; } = null!;

        [RqlProperty(DependsOn = new[] { nameof(C) })]
        public string B { get; set; } = null!;

        [RqlProperty(DependsOn = new[] { nameof(A) })]
        public string C { get; set; } = null!;
    }

    private class BranchedCycleView
    {
        [RqlProperty(DependsOn = new[] { nameof(B), nameof(C) })]
        public string A { get; set; } = null!;

        [RqlProperty(DependsOn = new[] { nameof(A) })]
        public string B { get; set; } = null!;

        [RqlProperty(DependsOn = new[] { nameof(D) })]
        public string C { get; set; } = null!;

        public string D { get; set; } = null!;
    }

    private class ContainerView
    {
        public string Id { get; set; } = null!;

        public Bag Bag { get; set; } = null!;

        public Item[] Items { get; set; } = null!;

        public DerivedItem Child { get; set; } = null!;
    }

    private class Bag : IEnumerable<int>
    {
        [RqlProperty(DependsOn = new[] { nameof(Raw) })]
        public string Value => Raw;

        [RqlProperty(Actions = RqlActions.None)]
        public string Raw { get; set; } = null!;

        public IEnumerator<int> GetEnumerator() => Enumerable.Empty<int>().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private class Item
    {
        [RqlProperty(DependsOn = new[] { nameof(Raw) })]
        public string Value => Raw;

        [RqlProperty(Actions = RqlActions.None)]
        public string Raw { get; set; } = null!;
    }

    private class BaseItem
    {
        [RqlProperty(DependsOn = new[] { nameof(Raw) })]
        public string Value => Raw;

        [RqlProperty(Actions = RqlActions.None)]
        public string Raw { get; set; } = null!;
    }

    private class DerivedItem : BaseItem
    {
        public string Id { get; set; } = null!;
    }
}
