#pragma warning disable IDE0130
using SoftwareOne.Rql.Abstractions;

namespace SoftwareOne.Rql;

public interface IRqlSelectInterceptor
{
    bool CanSelect(IRqlPropertyInfo property, Func<string> getFullPathCallback);
}
