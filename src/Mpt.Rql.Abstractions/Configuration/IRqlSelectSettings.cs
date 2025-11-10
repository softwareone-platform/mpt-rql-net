namespace Mpt.Rql.Abstractions.Configuration;

/// <summary>
/// Configuration for RQL select behavior
/// </summary>
public interface IRqlSelectSettings
{
    RqlSelectModes Implicit { get; set; }

    RqlSelectModes Explicit { get; set; }

    int? MaxDepth { get; set; }
}
