using SoftwareOne.Rql.Abstractions;

namespace SoftwareOne.Rql.Linq.Services.Context;

internal interface IBuilderContext
{
    RqlNode? CurrentNode { get; }
    void SetNode(RqlNode? node);
    bool TryGoToChild(IRqlPropertyInfo rqlProperty);
    void GoToRoot();
    string GetFullPath(string suffix);
}