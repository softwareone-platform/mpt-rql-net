using Mpt.Rql.Abstractions.Configuration;
using Mpt.Rql.Abstractions.Result;
using Mpt.Rql.Core;
using Mpt.Rql.Core.Metadata;
using Mpt.Rql.Services.Context;

namespace Mpt.Rql.Services.Ordering;

internal interface IOrderingPathInfoBuilder : IPathInfoBuilder { }

internal class OrderingPathInfoBuilder(IActionValidator actionValidator, IMetadataProvider metadataProvider, IBuilderContext builderContext, IRqlSettings settings)
    : PathInfoBuilder(metadataProvider, builderContext), IOrderingPathInfoBuilder
{
    private readonly IActionValidator _actionValidator = actionValidator;
    private readonly IBuilderContext _builderContext = builderContext;
    private readonly IRqlSettings _settings = settings;

    protected override Result<bool> ValidatePath(MemberPathInfo pathInfo)
    {
        if (!_actionValidator.Validate(pathInfo.PropertyInfo, RqlActions.Order))
            return Error.Validation("Ordering is not permitted.", _builderContext.GetFullPath(pathInfo.Path.ToString()));
        return true;
    }

    protected override bool UseSafeNavigation() => _settings.Ordering.SafeNavigation == SafeNavigationMode.On;
}
