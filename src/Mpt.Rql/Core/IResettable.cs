namespace Mpt.Rql.Core;

/// <summary>
/// Implemented by scoped services that hold per-request mutable state.
/// Enables reuse of the DI scope across multiple requests in worker scenarios.
/// </summary>
internal interface IResettable
{
    void Reset();
}
