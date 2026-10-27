using System.Linq.Expressions;
using FluentAssertions;
using Mpt.Rql.Services.Filtering;
using Xunit;

namespace Rql.Tests.Unit.Filtering;

public sealed class CollectionPredicateNormalizerTests
{
    private static readonly Expression<Func<SourceEntity, ResultView>> Projection =
        source => new ResultView
        {
            Id = source.Id,
            Label = source.Name ?? "none",
            Lines = source.IncludeLines
                ? source.Lines
                    .Select(line => new LineView { Sku = line.Sku })
                    .ToList()
                : null
        };

    public static TheoryData<Expression<Func<ResultView, bool>>, int[]>
        ConditionalPredicates => new()
        {
            {
                view => view.Lines != null &&
                    view.Lines.Any(line => line.Sku == "A"),
                [3, 4, 6]
            },
            {
                view => view.Lines != null &&
                    view.Lines.All(line => line.Sku == "A"),
                [2, 3, 4]
            },
            {
                view => !(view.Lines != null &&
                    view.Lines.Any(line => line.Sku == "A")),
                [1, 2, 5]
            },
            {
                view => view.Lines!.Any(line => line.Sku == "A"),
                [3, 4, 6]
            },
            {
                view => view.Lines!.All(line => line.Sku == "A"),
                [2, 3, 4]
            },
            {
                view => !view.Lines!.Any(line => line.Sku == "A"),
                [1, 2, 5]
            }
        };

    [Fact]
    public void Apply_MixedComputedPredicate_KeepsScalarAccessOnTheView()
    {
        var query = CreateSource().AsQueryable().Select(Projection);
        Expression<Func<ResultView, bool>> filter =
            view => view.Label == "none" &&
                view.Lines!.Any(line => line.Sku == "A");

        var normalized = CollectionPredicateNormalizer.Apply(query, filter);
        var result = normalized.ToList();

        var predicate = GetPredicate(normalized);

        predicate.Body.ToString()
            .Should().Contain("row.View.Label")
            .And.Contain("row.Source.Lines")
            .And.NotContain("row.Source.Name")
            .And.NotContain("row.View.Lines")
            .And.NotContain("ToList")
            .And.NotContain("Select(");

        result.Select(view => view.Id).Should().Equal(3, 6);
        result.Select(view => view.Label).Should().OnlyContain(
            label => label == "none");
    }

    [Theory]
    [MemberData(nameof(ConditionalPredicates))]
    public void Apply_ConditionalCollection_PreservesNullAndEmptySemantics(
        Expression<Func<ResultView, bool>> filter,
        int[] expectedIds)
    {
        var query = CreateSource().AsQueryable().Select(Projection);

        var normalized = CollectionPredicateNormalizer.Apply(query, filter);
        var result = normalized.ToList();

        result.Select(view => view.Id).Should().Equal(expectedIds);
    }

    [Fact]
    public void Apply_WithoutQuantifiers_KeepsTheOriginalProjection()
    {
        var query = CreateSource().AsQueryable().Select(Projection);
        Expression<Func<ResultView, bool>> filter =
            view => view.Label == "none";

        var normalized = CollectionPredicateNormalizer.Apply(query, filter);
        var result = normalized.ToList();

        var where = (MethodCallExpression)normalized.Expression;

        where.Method.Name.Should().Be(nameof(Queryable.Where));
        where.Arguments[0].Should().BeSameAs(query.Expression);
        GetPredicateFromWhere(where).Parameters[0].Type
            .Should().Be(typeof(ResultView));

        result.Select(view => view.Id).Should().Equal(1, 2, 3, 5, 6);
    }

    [Fact]
    public void Apply_WithoutProjection_FiltersTheViewDirectly()
    {
        var query = new[]
        {
            new ResultView
            {
                Id = 1,
                Lines = [new LineView { Sku = "A" }]
            },
            new ResultView
            {
                Id = 2,
                Lines = [new LineView { Sku = "B" }]
            }
        }.AsQueryable();

        Expression<Func<ResultView, bool>> filter =
            view => view.Lines!.Any(line => line.Sku == "A");

        var normalized = CollectionPredicateNormalizer.Apply(query, filter);
        var result = normalized.ToList();

        var where = (MethodCallExpression)normalized.Expression;

        where.Method.Name.Should().Be(nameof(Queryable.Where));
        where.Arguments[0].Should().BeSameAs(query.Expression);
        result.Select(view => view.Id).Should().Equal(1);
    }

    private static LambdaExpression GetPredicate(IQueryable<ResultView> query)
    {
        var select = (MethodCallExpression)query.Expression;
        var where = (MethodCallExpression)select.Arguments[0];

        return GetPredicateFromWhere(where);
    }

    private static LambdaExpression GetPredicateFromWhere(
        MethodCallExpression where)
        => (LambdaExpression)((UnaryExpression)where.Arguments[1]).Operand;

    private static List<SourceEntity> CreateSource()
    {
        return
        [
            new()
            {
                Id = 1,
                IncludeLines = false,
                Lines = [new SourceLine { Sku = "A" }]
            },
            new()
            {
                Id = 2
            },
            new()
            {
                Id = 3,
                Lines = [new SourceLine { Sku = "A" }]
            },
            new()
            {
                Id = 4,
                Name = "other",
                Lines = [new SourceLine { Sku = "A" }]
            },
            new()
            {
                Id = 5,
                Lines = [new SourceLine { Sku = "B" }]
            },
            new()
            {
                Id = 6,
                Lines =
                [
                    new SourceLine { Sku = "A" },
                    new SourceLine { Sku = "B" }
                ]
            }
        ];
    }

    public sealed class SourceEntity
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public bool IncludeLines { get; set; } = true;
        public List<SourceLine> Lines { get; set; } = [];
    }

    public sealed class SourceLine
    {
        public string Sku { get; set; } = string.Empty;
    }

    public sealed class ResultView
    {
        public int Id { get; set; }
        public string Label { get; set; } = string.Empty;
        public List<LineView>? Lines { get; set; }
    }

    public sealed class LineView
    {
        public string Sku { get; set; } = string.Empty;
    }
}