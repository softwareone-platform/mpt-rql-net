#pragma warning disable IDE0130
using Mpt.Rql.Abstractions;

namespace Mpt.Rql;

public interface IRqlSelectInterceptor
{
    bool CanSelect(IRqlPropertyInfo property, Func<string> getFullPathCallback);
}
