using Mpt.Rql;

namespace Rql.Tests.Integration.Core;

/// <summary>An entity carrying a keyed "parameter bag" — the primary use case for <c>first()</c>.</summary>
public class SupportCase
{
    [RqlProperty(IsCore = true)]
    public int Id { get; set; }

    [RqlProperty(IsCore = true)]
    public string Title { get; set; } = null!;

    public List<CaseParameter> Parameters { get; set; } = null!;
}

public enum ParameterKind
{
    Text = 0,
    Choice = 1,
    Number = 2,
}

/// <summary>Only <see cref="Name"/> is core: the other properties must reach the projection via the ordering graph.</summary>
public class CaseParameter
{
    [RqlProperty(IsCore = true)]
    public string Name { get; set; } = null!;

    public string? Value { get; set; }

    public Guid Key { get; set; }

    public ParameterKind Kind { get; set; }

    public int Rank { get; set; }
}
