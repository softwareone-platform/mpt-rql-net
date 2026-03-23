using Mpt.Rql;
using Rql.Tests.Integration.Core;
using Xunit;

namespace Rql.Tests.Integration.Tests.Functionality;

/// <summary>
/// Integration tests for <c>orderby()</c> with a <c>Name</c>/<c>Value</c> parameter collection —
/// the primary real-world use case:
///
/// <code>
///   +orderby(parameters, name, priority, value)
/// </code>
///
/// Each <see cref="SupportCase"/> carries a list of <see cref="CaseParameter"/> entries.
/// The sort key is the <c>Value</c> of the parameter whose <c>Name</c> equals the filter value.
/// <para>
/// <b>String ordering note:</b> Values are sorted lexicographically, not by semantic importance
/// (e.g. "critical" &lt; "high" &lt; "low" &lt; "medium" alphabetically).
/// When the sort key should reflect business priority, store a numeric rank alongside the label,
/// or use a different field as the result property.
/// </para>
/// </summary>
public class OrderByParameterValueTests
{
    private readonly IRqlQueryable<SupportCase, SupportCase> _rql;

    public OrderByParameterValueTests()
    {
        _rql = RqlFactory.Make<SupportCase>(services => { }, rql =>
        {
            rql.Settings.Mapping.Transparent = true;
            rql.Settings.Select.Implicit = RqlSelectModes.Core | RqlSelectModes.Primitive;
            rql.Settings.Select.Explicit = RqlSelectModes.All;
            rql.Settings.Select.MaxDepth = 5;
        });
    }

    // ── Shared dataset ───────────────────────────────────────────────────────

    /// <summary>
    /// SupportCases with the following priority values (alphabetical rank in parentheses):
    /// <list type="bullet">
    ///   <item>Id=1 → priority = "critical"  (rank 1)</item>
    ///   <item>Id=2 → priority = "high"      (rank 2) + extra "status" parameter</item>
    ///   <item>Id=3 → priority = "low"       (rank 3)</item>
    ///   <item>Id=4 → priority = "medium"    (rank 4)</item>
    ///   <item>Id=5 → no "priority" param    (null key)</item>
    ///   <item>Id=6 → empty parameters       (null key)</item>
    /// </list>
    /// </summary>
    private static IQueryable<SupportCase> MakeData() => new List<SupportCase>
    {
        new() { Id = 1, Title = "A", Parameters = [
            new CaseParameter { Name = "priority", Value = "critical" }
        ]},
        new() { Id = 2, Title = "B", Parameters = [
            new CaseParameter { Name = "priority", Value = "high"     },
            new CaseParameter { Name = "status",   Value = "open"     },  // extra param — ignored by filter
        ]},
        new() { Id = 3, Title = "C", Parameters = [
            new CaseParameter { Name = "priority", Value = "low"      }
        ]},
        new() { Id = 4, Title = "D", Parameters = [
            new CaseParameter { Name = "priority", Value = "medium"   }
        ]},
        new() { Id = 5, Title = "E", Parameters = [
            new CaseParameter { Name = "status",   Value = "pending"  }  // no "priority" → null key
        ]},
        new() { Id = 6, Title = "F", Parameters = [] },  // empty list → null key
    }.AsQueryable();

    // ── Ascending ────────────────────────────────────────────────────────────

    [Fact]
    public void OrderBy_StringValue_Ascending_NullsFirst_ThenLexicographic()
    {
        // Null keys (5, 6) sort before non-null keys.
        // Non-null keys are sorted lexicographically: "critical" < "high" < "low" < "medium"
        var result = _rql.Transform(MakeData(), new RqlRequest
        {
            Order = "+orderby(parameters,name,priority,value)"
        });

        Assert.True(result.IsSuccess);
        var ids = result.Query.Select(sc => sc.Id).ToList();
        Assert.Equal([5, 6, 1, 2, 3, 4], ids);
    }

    [Fact]
    public void OrderBy_StringValue_Ascending_NullKeysAreTrueNulls_NotEmptyString()
    {
        // FirstOrDefault on a non-matching Where returns null for strings (not "").
        // This test makes explicit that the null-sentinel is real null, not an empty string.
        var result = _rql.Transform(MakeData(), new RqlRequest
        {
            Order = "+orderby(parameters,name,priority,value)"
        });

        Assert.True(result.IsSuccess);
        var products = result.Query.ToList();

        // First two have no priority parameter — their sort key is null, not ""
        Assert.Equal(5, products[0].Id);
        Assert.Equal(6, products[1].Id);
        Assert.All(products.Take(2), sc =>
            Assert.DoesNotContain(sc.Parameters, p => p.Name == "priority"));
    }

    // ── Descending ───────────────────────────────────────────────────────────

    [Fact]
    public void OrderBy_StringValue_Descending_NullsLast_ThenReverseLexicographic()
    {
        // Non-null keys in reverse lex order: "medium" > "low" > "high" > "critical"
        // Null keys come last.
        var result = _rql.Transform(MakeData(), new RqlRequest
        {
            Order = "-orderby(parameters,name,priority,value)"
        });

        Assert.True(result.IsSuccess);
        var ids = result.Query.Select(sc => sc.Id).ToList();
        Assert.Equal([4, 3, 2, 1, 5, 6], ids);
    }

    // ── Extra parameters on the same entity ─────────────────────────────────

    [Fact]
    public void OrderBy_IgnoresParametersNotMatchingFilter()
    {
        // SupportCase 2 has both "priority"="high" and "status"="open".
        // Only the priority value should influence the sort; "status" is ignored.
        var result = _rql.Transform(MakeData(), new RqlRequest
        {
            Order = "+orderby(parameters,name,priority,value)"
        });

        Assert.True(result.IsSuccess);
        var sc2 = result.Query.Single(sc => sc.Id == 2);
        // Verify Id=2 lands in position 4 (after critical, not bumped by "status"="open")
        var ids = result.Query.Select(sc => sc.Id).ToList();
        Assert.Equal(3, ids.IndexOf(2));  // zero-based index 3 → fourth position after two nulls + "critical"
    }

    // ── Null value in matching parameter ────────────────────────────────────

    [Fact]
    public void OrderBy_ParameterExistsButValueIsNull_TreatedAsNullKey()
    {
        // A parameter with the right Name but a null Value should sort as null (first ascending).
        var data = new List<SupportCase>
        {
            new() { Id = 1, Title = "A", Parameters = [new CaseParameter { Name = "priority", Value = "high" }] },
            new() { Id = 2, Title = "B", Parameters = [new CaseParameter { Name = "priority", Value = null  }] },
        }.AsQueryable();

        var result = _rql.Transform(data, new RqlRequest
        {
            Order = "+orderby(parameters,name,priority,value)"
        });

        Assert.True(result.IsSuccess);
        var ids = result.Query.Select(sc => sc.Id).ToList();
        // Null value → null sort key → first ascending
        Assert.Equal([2, 1], ids);
    }

    // ── String ordering is lexicographic, not semantic ───────────────────────

    [Fact]
    public void OrderBy_StringOrdering_IsLexicographic_NotNumeric()
    {
        // Demonstrates that string ordering is character-by-character.
        // "10" < "9" lexicographically (because '1' < '9') even though 10 > 9 numerically.
        var data = new List<SupportCase>
        {
            new() { Id = 1, Title = "A", Parameters = [new CaseParameter { Name = "rank", Value = "9"  }] },
            new() { Id = 2, Title = "B", Parameters = [new CaseParameter { Name = "rank", Value = "10" }] },
            new() { Id = 3, Title = "C", Parameters = [new CaseParameter { Name = "rank", Value = "2"  }] },
        }.AsQueryable();

        var result = _rql.Transform(data, new RqlRequest
        {
            Order = "+orderby(parameters,name,rank,value)"
        });

        Assert.True(result.IsSuccess);
        var ids = result.Query.Select(sc => sc.Id).ToList();
        // Lex order: "10" < "2" < "9"  →  ids = [2, 3, 1]
        Assert.Equal([2, 3, 1], ids);
    }

    // ── Filter is case-sensitive ─────────────────────────────────────────────

    [Fact]
    public void OrderBy_FilterNameIsCaseSensitive()
    {
        // "Priority" (capital P) does not match filter value "priority" (lowercase).
        var data = new List<SupportCase>
        {
            new() { Id = 1, Title = "A", Parameters = [new CaseParameter { Name = "priority", Value = "high" }] },
            new() { Id = 2, Title = "B", Parameters = [new CaseParameter { Name = "Priority", Value = "low"  }] },
        }.AsQueryable();

        var result = _rql.Transform(data, new RqlRequest
        {
            Order = "+orderby(parameters,name,priority,value)"  // filter = "priority" (lowercase)
        });

        Assert.True(result.IsSuccess);
        var ids = result.Query.Select(sc => sc.Id).ToList();
        // Id=1 has lowercase "priority" → key="high" → comes last ascending
        // Id=2 has "Priority" (capital P) → no match → null key → comes first
        Assert.Equal([2, 1], ids);
    }

    // ── Combined with scalar sort for tie-breaking ───────────────────────────

    [Fact]
    public void OrderBy_CombinedWithScalarSort_BreaksTiesById()
    {
        // Cases 5 and 6 both have null priority keys.
        // Secondary sort by -id (descending) should order them: 6, 5.
        var result = _rql.Transform(MakeData(), new RqlRequest
        {
            Order = "+orderby(parameters,name,priority,value),-id"
        });

        Assert.True(result.IsSuccess);
        var ids = result.Query.Select(sc => sc.Id).ToList();

        // Null-key group first, ordered by id descending: 6, 5
        Assert.Equal(6, ids[0]);
        Assert.Equal(5, ids[1]);

        // Non-null group sorted lexicographically ascending (primary key unchanged)
        Assert.Equal([1, 2, 3, 4], ids.Skip(2).ToList());
    }
}
