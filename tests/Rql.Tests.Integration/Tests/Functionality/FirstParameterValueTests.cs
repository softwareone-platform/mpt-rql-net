using Mpt.Rql;
using Mpt.Rql.Abstractions.Configuration;
using Rql.Tests.Integration.Core;
using Xunit;

namespace Rql.Tests.Integration.Tests.Functionality;

/// <summary>
/// The primary use case: <c>+first(parameters,eq(name,priority)).value</c> over a keyed parameter bag.
/// Values sort lexicographically ("critical" &lt; "high" &lt; "low" &lt; "medium").
/// </summary>
public class FirstParameterValueTests
{
    private static readonly Guid PriorityKey = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid StatusKey = new("22222222-2222-2222-2222-222222222222");

    private static IRqlQueryable<SupportCase, SupportCase> MakeTransparent(NavigationStrategy navigation = NavigationStrategy.Default) =>
        RqlFactory.Make<SupportCase>(services => { }, rql =>
        {
            rql.Settings.Mapping.Transparent = true;
            rql.Settings.Ordering.Navigation = navigation;
            rql.Settings.Select.Implicit = RqlSelectModes.Core | RqlSelectModes.Primitive;
            rql.Settings.Select.Explicit = RqlSelectModes.All;
            rql.Settings.Select.MaxDepth = 5;
        });

    /// <summary>Mapping ON with Core-only selection: the sort key's columns must reach the projection via the graph.</summary>
    private static IRqlQueryable<SupportCase, SupportCase> MakeMapped() =>
        RqlFactory.Make<SupportCase>(services => { }, rql =>
        {
            rql.Settings.Mapping.Transparent = false;
            rql.Settings.Select.Implicit = RqlSelectModes.Core;
            rql.Settings.Select.Explicit = RqlSelectModes.Core;
            rql.Settings.Select.MaxDepth = 5;
        });

    /// <summary>
    /// Id=1 critical (rank -2) · Id=2 high (rank -1) + status · Id=3 low (rank 1) · Id=4 medium (rank 2) ·
    /// Id=5 no priority parameter · Id=6 empty bag.
    /// Ranks straddle zero on purpose: a missing element must sort as <c>null</c>, not as <c>default(int)</c> = 0.
    /// </summary>
    private static IQueryable<SupportCase> Data() => new List<SupportCase>
    {
        new() { Id = 1, Title = "A", Parameters = [Priority("critical", -2)] },
        new() { Id = 2, Title = "B", Parameters = [Priority("high", -1), new CaseParameter { Name = "status", Key = StatusKey, Kind = ParameterKind.Text, Value = "open" }] },
        new() { Id = 3, Title = "C", Parameters = [Priority("low", 1)] },
        new() { Id = 4, Title = "D", Parameters = [Priority("medium", 2)] },
        new() { Id = 5, Title = "E", Parameters = [new CaseParameter { Name = "status", Key = StatusKey, Kind = ParameterKind.Text, Value = "closed" }] },
        new() { Id = 6, Title = "F", Parameters = [] },
    }.AsQueryable();

    private static CaseParameter Priority(string value, int rank)
        => new() { Name = "priority", Key = PriorityKey, Kind = ParameterKind.Choice, Value = value, Rank = rank };

    private static List<int> Ids(RqlResponse<SupportCase> result)
    {
        Assert.True(result.IsSuccess, string.Join("; ", result.Errors));
        return result.Query.Select(c => c.Id).ToList();
    }

    [Fact]
    public void ByName_Ascending()
        => Assert.Equal([5, 6, 1, 2, 3, 4], Ids(MakeTransparent().Transform(Data(), new RqlRequest { Order = "+first(parameters,eq(name,priority)).value" })));

    [Fact]
    public void ByName_Descending()
        => Assert.Equal([4, 3, 2, 1, 5, 6], Ids(MakeTransparent().Transform(Data(), new RqlRequest { Order = "-first(parameters,eq(name,priority)).value" })));

    [Fact]
    public void GuidPredicateValue_UsesTheFilterPipelineConverter()
        => Assert.Equal([5, 6, 1, 2, 3, 4], Ids(MakeTransparent().Transform(Data(), new RqlRequest { Order = $"+first(parameters,eq(key,{PriorityKey})).value" })));

    [Fact]
    public void EnumPredicateValue_UsesTheFilterPipelineConverter()
        => Assert.Equal([5, 6, 1, 2, 3, 4], Ids(MakeTransparent().Transform(Data(), new RqlRequest { Order = "+first(parameters,eq(kind,Choice)).value" })));

    [Fact]
    public void ValueTypeSelector_MissingElementSortsAsNull_NotZero()
        // rank: 1→-2, 2→-1, 3→1, 4→2, 5→null, 6→null.
        // Lifted:   null, null, -2, -1, 1, 2  → [5, 6, 1, 2, 3, 4]
        // Unlifted: -2, -1, 0, 0, 1, 2        → [1, 2, 5, 6, 3, 4]  (the bug this guards against)
        => Assert.Equal([5, 6, 1, 2, 3, 4], Ids(MakeTransparent().Transform(Data(), new RqlRequest { Order = "+first(parameters,eq(name,priority)).rank" })));

    [Fact]
    public void CombinedWithScalar_TieBreaksNullKeys()
        => Assert.Equal([6, 5, 1, 2, 3, 4], Ids(MakeTransparent().Transform(Data(), new RqlRequest { Order = "+first(parameters,eq(name,priority)).value,-id" })));

    [Fact]
    public void MappingEnabled_PredicateComparingTwoElementProperties_ProjectsBothColumns()
    {
        // eq(name,value) compares two element properties; the right-hand column must reach the projection too.
        var data = new List<SupportCase>
        {
            new() { Id = 1, Title = "A", Parameters = [new CaseParameter { Name = "x", Value = "x", Rank = 1 }] },
            new() { Id = 2, Title = "B", Parameters = [new CaseParameter { Name = "x", Value = "y", Rank = 2 }] },
        }.AsQueryable();

        // only case 1 matches → case 2 has a null key and sorts first
        Assert.Equal([2, 1], Ids(MakeMapped().Transform(data, new RqlRequest { Order = "+first(parameters,eq(name,value)).rank" })));
    }

    // ── Mapping enabled: the graph must carry parameters.name and parameters.value into the projection ──

    [Fact]
    public void MappingEnabled_CoreOnlySelection_StillSortsCorrectly()
    {
        var result = MakeMapped().Transform(Data(), new RqlRequest { Order = "+first(parameters,eq(name,priority)).value" });

        Assert.Equal([5, 6, 1, 2, 3, 4], Ids(result));
    }

    [Fact]
    public void MappingEnabled_ProjectsExactlyTheColumnsTheKeyReads()
    {
        var cases = MakeMapped().Transform(Data(), new RqlRequest { Order = "+first(parameters,eq(name,priority)).value" }).Query.ToList();

        var critical = cases.Single(c => c.Id == 1).Parameters.Single();
        Assert.Equal("priority", critical.Name);      // predicate column
        Assert.Equal("critical", critical.Value);     // selector column
        Assert.Equal(Guid.Empty, critical.Key);       // not requested → not projected
        Assert.Equal(0, critical.Rank);               // not requested → not projected
    }

    [Fact]
    public void MappingEnabled_ArgumentsDoNotLeakIntoTheRootProjection()
    {
        // 'value' and 'name' are also plausible root-level names; with the old design they would have been
        // pulled into the root projection. Here only Id/Title (core) and parameters (hierarchy) are projected.
        var result = MakeMapped().Transform(Data(), new RqlRequest { Order = "+first(parameters,eq(name,priority)).value" });

        Assert.True(result.IsSuccess);
        Assert.False(result.Graph.TryGetChild("value", out _));
        Assert.False(result.Graph.TryGetChild("name", out _));
        Assert.True(result.Graph.TryGetChild("parameters", out var parameters));
        Assert.True(parameters!.TryGetChild("name", out _));
        Assert.True(parameters.TryGetChild("value", out _));
    }
}
