using FluentAssertions;
using Mpt.Rql.Services.Filtering.Binding;
using System.Linq.Expressions;
using Xunit;

namespace Rql.Tests.Unit.Filtering.Binding;

/// <summary>Two-valued conditionals (MapIf / Switch mappings) are not guards; they must survive the rewrite well-typed.</summary>
public class MapIfShapeTests
{
    [Fact]
    public void ValueTypeBranchesThroughAGuard_StayWellTypedAndEquivalent()
    {
        // MapIf: Count = s.Case != null ? s.Case.Id : s.Fallback, with the first branch null-propagated and
        // converted back to int the way MakeBindExpression does under safe mapping
        Expression<Func<Source, View>> selector = s => new View
        {
            Id = s.Id,
            Count = s.Case != null ? (int)(s.Case == null ? null : (int?)s.Case.Id)! : s.Fallback
        };
        var data = new[]
        {
            new Source { Id = 1, Case = new Nested { Id = 10 }, Fallback = 1 },
            new Source { Id = 2, Case = null, Fallback = 10 },
            new Source { Id = 3, Case = null, Fallback = 2 },
        };

        var bound = StoragePredicateBinder.Bind(v => v.Count == 10, selector);

        var expected = data.AsQueryable().Select(selector).Where(v => v.Count == 10).Select(v => v.Id).ToList();
        data.AsQueryable().Where(bound).Select(s => s.Id).Should().Equal(1, 2);
        data.AsQueryable().Where(bound).Select(s => s.Id).Should().Equal(expected);
    }

    [Fact]
    public void ReferenceBranches_AreLeftAsAConditional()
    {
        Expression<Func<Source, View>> selector = s => new View { Id = s.Id, Label = s.Fallback > 5 ? s.Name : "low" };
        var data = new[]
        {
            new Source { Id = 1, Name = "a", Fallback = 9 },
            new Source { Id = 2, Name = "b", Fallback = 1 },
        };

        var bound = StoragePredicateBinder.Bind(v => v.Label == "low", selector);

        bound.Body.ToString().Should().Be("(IIF((s.Fallback > 5), s.Name, \"low\") == \"low\")");
        data.AsQueryable().Where(bound).Select(s => s.Id).Should().Equal(2);
    }

    public class Source
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public int Fallback { get; set; }
        public Nested? Case { get; set; }
    }

    public class Nested
    {
        public int Id { get; set; }
    }

    public class View
    {
        public int Id { get; set; }
        public int Count { get; set; }
        public string? Label { get; set; }
    }
}
