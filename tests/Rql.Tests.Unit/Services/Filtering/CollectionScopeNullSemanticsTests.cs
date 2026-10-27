using System.Linq.Expressions;
using FluentAssertions;
using Mpt.Rql.Services.Filtering;
using Xunit;

namespace Rql.Tests.Unit.Services.Filtering;

public sealed class CollectionScopeNullSemanticsTests
{
    private static readonly Expression<Func<Storage, View>> Projection =
        storage => new View
        {
            Values = storage.Values != null
                ? storage.Values
                    .Where(value => value != 0)
                    .Select(value => value)
                    .ToList()
                : null
        };

    public static TheoryData<List<int>?, bool, bool> Collections => new()
    {
        { null, false, false },
        { new List<int>(), false, true },
        { new List<int> { 0 }, false, true },
        { new List<int> { 1 }, true, true },
        { new List<int> { 2 }, false, false },
        { new List<int> { 1, 2 }, true, false }
    };
    
    [Theory]
    [MemberData(nameof(Collections))]
    public void ToSourceFilter_PreservesGuardedAnyAndAll(
        List<int>? values,
        bool expectedAny,
        bool expectedAll)
    {
        var source = new Storage { Values = values };

        Expression<Func<View, bool>> any =
            view => view.Values != null &&
                    view.Values.Any(value => value == 1);

        Expression<Func<View, bool>> all =
            view => view.Values != null &&
                    view.Values.All(value => value == 1);

        var originalView = Projection.Compile()(source);

        var rewrittenAny = Rewrite(any);
        var rewrittenAll = Rewrite(all);

        var anyResult = rewrittenAny(source);
        var allResult = rewrittenAll(source);

        anyResult.Should().Be(expectedAny);
        allResult.Should().Be(expectedAll);
        anyResult.Should().Be(any.Compile()(originalView));
        allResult.Should().Be(all.Compile()(originalView));
    }

    public static TheoryData<List<int>?, bool> NegatedAnyCollections => new()
    {
        { null, true },
        { new List<int>(), true },
        { new List<int> { 0 }, true },
        { new List<int> { 1 }, false },
        { new List<int> { 2 }, true },
        { new List<int> { 1, 2 }, false }
    };
    
    [Theory]
    [MemberData(nameof(NegatedAnyCollections))]
    public void ToSourceFilter_PreservesNegatedGuardedAny(
        List<int>? values,
        bool expectedResult)
    {
        var source = new Storage { Values = values };

        Expression<Func<View, bool>> filter =
            view => !(view.Values != null &&
                      view.Values.Any(value => value == 1));

        var original = filter.Compile();
        var projected = Projection.Compile()(source);
        var rewritten = Rewrite(filter);

        var result = rewritten(source);

        result.Should().Be(expectedResult);
        result.Should().Be(original(projected));
    }

    [Theory]
    [MemberData(nameof(Collections))]
    public void ToSourceFilter_ConditionalCollection_EvaluatesSelectedBranch(
        List<int>? values,
        bool expectedAny,
        bool expectedAll)
    {
        var source = new Storage { Values = values };

        Expression<Func<View, bool>> any =
            view => view.Values!.Any(value => value == 1);

        Expression<Func<View, bool>> all =
            view => view.Values!.All(value => value == 1);

        var rewrittenAny = Rewrite(any);
        var rewrittenAll = Rewrite(all);

        var anyResult = rewrittenAny(source);
        var allResult = rewrittenAll(source);

        anyResult.Should().Be(expectedAny);
        allResult.Should().Be(expectedAll);
    }
    
    [Fact]
    public void ToSourceFilter_UnguardedAllOnDirectNullCollection_StillThrows()
    {
        Expression<Func<Storage, View>> projection =
            storage => new View
            {
                Values = storage.Values
            };

        Expression<Func<View, bool>> filter =
            view => view.Values!.All(value => value == 1);

        var source = new Storage { Values = null };
        var projected = projection.Compile()(source);
        var original = filter.Compile();

        var rewritten = ((Expression<Func<Storage, bool>>)
                FilterPushdownVisitor.ToSourceFilter(
                    filter,
                    projection,
                    removeRedundantNullChecks: false))
            .Compile();

        Action originalCall = () => { original(projected); };
        Action rewrittenCall = () => { rewritten(source); };

        originalCall.Should().Throw<ArgumentNullException>();
        rewrittenCall.Should().Throw<ArgumentNullException>();
    }
    


    [Fact]
    public void ToSourceFilter_UnguardedAnyOnDirectNullCollection_StillThrows()
    {
        Expression<Func<Storage, View>> projection =
            storage => new View
            {
                Values = storage.Values
            };

        Expression<Func<View, bool>> filter =
            view => view.Values!.Any(value => value == 1);

        var source = new Storage { Values = null };
        var projected = projection.Compile()(source);
        var original = filter.Compile();

        var rewritten = ((Expression<Func<Storage, bool>>)
                FilterPushdownVisitor.ToSourceFilter(
                    filter,
                    projection,
                    removeRedundantNullChecks: false))
            .Compile();

        Action originalCall = () => { original(projected); };
        Action rewrittenCall = () => { rewritten(source); };

        originalCall.Should().Throw<ArgumentNullException>();
        rewrittenCall.Should().Throw<ArgumentNullException>();
    }
    
    [Fact]
    public void ToSourceFilter_NormalizesArrayMaterialization()
    {
        Expression<Func<Storage, ArrayView>> projection =
            storage => new ArrayView
            {
                Values = storage.Values!
                    .Where(value => value != 0)
                    .Select(value => value)
                    .ToArray()
            };

        Expression<Func<ArrayView, bool>> filter =
            view => view.Values.Any(value => value == 1);

        var source = new Storage { Values = [0, 1, 2] };

        var expression = (Expression<Func<Storage, bool>>)
            FilterPushdownVisitor.ToSourceFilter(
                filter,
                projection,
                removeRedundantNullChecks: false);

        var result = expression.Compile()(source);

        result.Should().BeTrue();
        expression.Body.ToString().Should().NotContain("ToArray");
    }

    private static Func<Storage, bool> Rewrite(
        Expression<Func<View, bool>> filter)
    {
        return ((Expression<Func<Storage, bool>>)
            FilterPushdownVisitor.ToSourceFilter(
                filter,
                Projection,
                removeRedundantNullChecks: false))
            .Compile();
    }

    public sealed class Storage
    {
        public List<int>? Values { get; set; }
    }

    public sealed class View
    {
        public List<int>? Values { get; set; }
    }

    public sealed class ArrayView
    {
        public int[] Values { get; set; } = [];
    }
}