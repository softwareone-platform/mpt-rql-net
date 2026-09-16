using Mpt.Rql;
using System.Collections.Immutable;

namespace Rql.Tests.Integration.Core;

/// <summary>Entity whose collection is a struct enumerable — not reference-assignable to IEnumerable&lt;T&gt;.</summary>
public class FrozenCase
{
    [RqlProperty(IsCore = true)]
    public int Id { get; set; }

    public ImmutableArray<CaseParameter> Parameters { get; set; }
}
