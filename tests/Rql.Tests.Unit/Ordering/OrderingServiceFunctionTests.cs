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
using Rql.Tests.Unit.Services.Models;
using System.Linq.Expressions;
using Xunit;

namespace Rql.Tests.Unit.Ordering;

/// <summary>
/// Drives <see cref="OrderingService{TView}"/> end to end with real graph/path builders and a mocked
/// filtering builder (predicate is always <c>e.Name == "x"</c>), then applies the produced
/// transformations to in-memory data.
/// </summary>
public class OrderingServiceFunctionTests
{
    private readonly QueryContext<Product> _queryContext;
    private readonly OrderingService<Product> _service;

    public OrderingServiceFunctionTests()
    {
        var validator = new Mock<IActionValidator>();
        validator.Setup(v => v.Validate(It.IsAny<RqlPropertyInfo>(), It.IsAny<RqlActions>())).Returns(true);

        var accessor = new ExternalServiceAccessor();
        accessor.SetServiceProvider(new ServiceCollection().BuildServiceProvider());
        _queryContext = new QueryContext<Product>(accessor);

        var metadata = new MetadataProvider(new PropertyNameProvider(), new MetadataFactory(new GlobalRqlSettings()));
        var builderContext = new BuilderContext();
        var settings = new RqlSettings();
        var registry = new OrderingFunctionRegistry([new FirstOrderingFunction()]);

        var filteringGraph = new FilteringGraphBuilder<Product>(metadata, validator.Object, builderContext);
        var orderingGraph = new OrderingGraphBuilder<Product>(metadata, validator.Object, builderContext, filteringGraph, registry);
        var pathBuilder = new OrderingPathInfoBuilder(validator.Object, metadata, builderContext, settings, Mock.Of<IExternalServiceAccessor>());

        var filterBuilder = new Mock<IExpressionBuilder>();
        filterBuilder
            .Setup(b => b.Build(It.IsAny<ParameterExpression>(), It.IsAny<RqlExpression>()))
            .Returns((ParameterExpression e, RqlExpression _) =>
                new Result<Expression>(Expression.Equal(Expression.Property(e, nameof(Item.Name)), Expression.Constant("x"))));

        var services = new OrderingFunctionServices(pathBuilder, filterBuilder.Object, builderContext, settings, registry);
        _service = new OrderingService<Product>(_queryContext, orderingGraph, new RqlParser(), services);
    }

    /// <summary>
    /// P1: matching item id 30 · P2: matching item id 10 · P3: no match (null key) · P4: no items (null key).
    /// </summary>
    private static IQueryable<Product> Data() => new List<Product>
    {
        new() { Id = 1, Items = [new Item { Id = 30, Name = "x" }] },
        new() { Id = 2, Items = [new Item { Id = 10, Name = "x" }] },
        new() { Id = 3, Items = [new Item { Id = 20, Name = "y" }] },
        new() { Id = 4, Items = [] },
    }.AsQueryable();

    private List<int> Run(string order)
    {
        _service.Process(order);
        _queryContext.HasErrors.Should().BeFalse(string.Join("; ", _queryContext.GetErrors()));
        return _queryContext.ApplyTransformations(Data()).Select(p => p.Id).ToList();
    }

    [Fact]
    public void FunctionThenScalar_OrdersByKeyAscThenTieBreaks()
        // null keys first (P3, P4 ordered by -id => 4, 3), then key 10 (P2), key 30 (P1)
        => Run("+first(items,id,eq(name,x)),-id").Should().Equal(4, 3, 2, 1);

    [Fact]
    public void FunctionDescending_PutsNullKeysLast()
        => Run("-first(items,id,eq(name,x))").Should().Equal(1, 2, 3, 4);

    [Fact]
    public void NoSign_MeansAscending()
        => Run("first(items,id,eq(name,x))").Should().Equal(3, 4, 2, 1);

    [Fact]
    public void ScalarThenFunction_UsesThenBy()
        // all ids distinct, so the scalar decides; the function must still build (ThenBy path)
        => Run("-id,+first(items,id,eq(name,x))").Should().Equal(4, 3, 2, 1);

    [Fact]
    public void TwoArgumentForm_Works()
        // first item id: P1 30, P2 10, P3 20, P4 null
        => Run("+first(items,id)").Should().Equal(4, 2, 3, 1);

    [Fact]
    public void UnknownFunction_ReportsUnknownFunctionCode()
    {
        _service.Process("+nope(items,id)");

        var error = _queryContext.GetErrors().Single();
        error.Code.Should().Be(OrderingErrorCodes.UnknownFunction);
        error.Message.Should().Be("Unknown ordering function 'nope'.");
    }

    [Fact]
    public void WrongArity_ReportsFunctionArgumentsCode()
    {
        _service.Process("+first(items)");

        _queryContext.GetErrors().Single().Code.Should().Be(OrderingErrorCodes.FunctionArguments);
    }

    [Fact]
    public void UnknownSelector_ReportsFullPath()
    {
        _service.Process("+first(items,nope,eq(name,x))");

        var error = _queryContext.GetErrors().Single();
        error.Message.Should().Be("Invalid property path.");
        error.Path.Should().Be("items.nope");
    }

    [Fact]
    public void FailingFunction_DoesNotStopOtherItems()
    {
        _service.Process("+nope(items,id),-id");

        _queryContext.GetErrors().Should().ContainSingle(e => e.Code == OrderingErrorCodes.UnknownFunction);
        _queryContext.ApplyTransformations(Data()).Select(p => p.Id).Should().Equal(4, 3, 2, 1);
    }

    [Fact]
    public void PlainOrderString_StillWorks()
        => Run("-id").Should().Equal(4, 3, 2, 1);

    [Fact]
    public void SignOnlyGroup_SortsByItsItems()
        // "-(id)" is a plain list of order terms (the group sign is ignored, as before this feature), not a function call
        => Run("-(id)").Should().Equal(1, 2, 3, 4);

    [Fact]
    public void MalformedOrder_ReportsValidationErrorInsteadOfThrowing()
    {
        _service.Process("+first(items,id,eq(name))");

        var error = _queryContext.GetErrors().Single();
        error.Code.Should().Be("order:malformed");
        error.Message.Should().StartWith("Malformed order expression:");
    }
}
