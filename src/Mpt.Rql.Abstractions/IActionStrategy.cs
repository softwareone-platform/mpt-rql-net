#pragma warning disable IDE0130
using Mpt.Rql.Abstractions;

namespace Mpt.Rql;

public interface IActionStrategy
{
    bool IsAllowed(RqlActions action);
    bool IsAllowed(RqlActions action, IRqlPropertyInfo property) => IsAllowed(action);
}
