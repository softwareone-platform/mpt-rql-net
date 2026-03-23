using Mpt.Rql;

namespace Rql.Tests.Integration.Core;

public class SupportCase
{
    [RqlProperty(IsCore = true)]
    public int Id { get; set; }

    [RqlProperty(IsCore = true)]
    public string Title { get; set; } = null!;

    public List<CaseParameter> Parameters { get; set; } = null!;
}

public class CaseParameter
{
    [RqlProperty(IsCore = true)]
    public string Name { get; set; } = null!;

    [RqlProperty(IsCore = true)]
    public string? Value { get; set; }
}
