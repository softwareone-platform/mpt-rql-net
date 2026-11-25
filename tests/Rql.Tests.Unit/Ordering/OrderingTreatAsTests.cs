using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Mpt.Rql;
using Mpt.Rql.Abstractions;
using Mpt.Rql.Core;
using Mpt.Rql.Core.Metadata;
using Mpt.Rql.Parsers.Linear.Services;
using Mpt.Rql.Services.Context;
using Mpt.Rql.Services.Filtering;
using Mpt.Rql.Services.Filtering.Builders;
using Mpt.Rql.Services.Ordering;
using Mpt.Rql.Services.Ordering.Functions;
using Mpt.Rql.Settings;
using System.Linq.Expressions;
using Xunit;

namespace Rql.Tests.Unit.Ordering;

/// <summary>
/// <c>TreatAs</c> only steers default selection; whether a property can be a sort key (or the collection of
/// <c>first()</c>) is decided by its real type.
/// </summary>
public class OrderingTreatAsTests
{
    private readonly QueryContext<TreatAsRoot> _queryContext;
    private readonly OrderingService<TreatAsRoot> _service;

    public OrderingTreatAsTests()
    {
        var validator = new Mock<IActionValidator>();
        validator.Setup(v => v.Validate(It.IsAny<RqlPropertyInfo>(), It.IsAny<RqlActions>())).Returns(true);

        var accessor = new ExternalServiceAccessor();
        accessor.SetServiceProvider(new ServiceCollection().BuildServiceProvider());
        _queryContext = new QueryContext<TreatAsRoot>(accessor);

        var metadata = new MetadataProvider(new PropertyNameProvider(), new MetadataFactory(new GlobalRqlSettings()));
        var builderContext = new BuilderContext();
        var settings = new RqlSettings();
        var registry = new OrderingFunctionRegistry([new FirstOrderingFunction()]);

        var filteringGraph = new FilteringGraphBuilder<TreatAsRoot>(metadata, validator.Object, builderContext);
        var orderingGraph = new OrderingGraphBuilder<TreatAsRoot>(metadata, validator.Object, builderContext, filteringGraph, registry);
        var pathBuilder = new OrderingPathInfoBuilder(validator.Object, metadata, builderContext, settings, Mock.Of<IExternalServiceAccessor>());

        var services = new OrderingFunctionServices(pathBuilder, Mock.Of<IExpressionBuilder>(), builderContext, settings, registry);
        _service = new OrderingService<TreatAsRoot>(_queryContext, orderingGraph, new RqlParser(), services);
    }

    private static IQueryable<TreatAsRoot> Data() => new List<TreatAsRoot>
    {
        new() { Id = 1, Code = "b", Items = [new TreatAsItem { Id = 20, Code = "y" }] },
        new() { Id = 2, Code = "c", Items = [new TreatAsItem { Id = 30, Code = "z" }] },
        new() { Id = 3, Code = "a", Items = [new TreatAsItem { Id = 10, Code = "x" }] },
    }.AsQueryable();

    private List<int> Run(string order)
    {
        _service.Process(order);
        _queryContext.HasErrors.Should().BeFalse(string.Join("; ", _queryContext.GetErrors()));
        return _queryContext.ApplyTransformations(Data()).Select(p => p.Id).ToList();
    }

    [Fact]
    public void PrimitiveTreatedAsCollection_CanBeOrdered()
        => Run("code").Should().Equal(3, 1, 2);

    [Fact]
    public void ReferenceTreatedAsPrimitive_IsRejected()
    {
        _service.Process("category");

        var error = _queryContext.GetErrors().Single();
        error.Code.Should().Be(OrderingErrorCodes.NotPrimitive);
        error.Path.Should().Be("category");
    }

    [Fact]
    public void First_OverCollectionTreatedAsReference_Works()
        => Run("-first(items).id").Should().Equal(2, 1, 3);

    [Fact]
    public void First_SelectorPrimitiveTreatedAsCollection_Works()
        => Run("first(items).code").Should().Equal(3, 1, 2);

    [Fact]
    public void First_OverReferenceTreatedAsCollection_ReportsNotCollection()
    {
        _service.Process("first(hidden).id");

        _queryContext.GetErrors().Single().Code.Should().Be(OrderingErrorCodes.NotCollection);
    }

    internal class TreatAsRoot
    {
        [RqlProperty(IsCore = true)]
        public int Id { get; set; }

        [RqlProperty(TreatAs = RqlPropertyType.Collection)]
        public string Code { get; set; } = null!;

        [RqlProperty(TreatAs = RqlPropertyType.Primitive)]
        public TreatAsItem Category { get; set; } = null!;

        [RqlProperty(TreatAs = RqlPropertyType.Collection)]
        public TreatAsItem Hidden { get; set; } = null!;

        [RqlProperty(TreatAs = RqlPropertyType.Reference)]
        public List<TreatAsItem> Items { get; set; } = null!;
    }

    internal class TreatAsItem
    {
        [RqlProperty(IsCore = true)]
        public int Id { get; set; }

        [RqlProperty(TreatAs = RqlPropertyType.Collection)]
        public string Code { get; set; } = null!;
    }
}
