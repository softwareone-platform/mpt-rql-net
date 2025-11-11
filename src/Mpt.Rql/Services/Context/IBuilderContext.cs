using Mpt.Rql.Abstractions;

namespace Mpt.Rql.Services.Context;

internal interface IBuilderContext
{
    RqlNode? CurrentNode { get; }
    void SetNode(RqlNode? node);
    bool TryGoToChild(IRqlPropertyInfo rqlProperty);
    void GoToRoot();
    string GetFullPath(string suffix);
}