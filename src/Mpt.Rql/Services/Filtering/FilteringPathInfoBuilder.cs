using Mpt.Rql.Abstractions.Configuration;
using Mpt.Rql.Abstractions.Result;
using Mpt.Rql.Core;
using Mpt.Rql.Core.Metadata;
using Mpt.Rql.Services.Context;

namespace Mpt.Rql.Services.Filtering;

internal interface IFilteringPathInfoBuilder : IPathInfoBuilder { }

internal class FilteringPathInfoBuilder(IActionValidator actionValidator, IMetadataProvider metadataProvider, IBuilderContext builderContext, IRqlSettings settings)
    : PathInfoBuilder(metadataProvider, builderContext), IFilteringPathInfoBuilder
{
    private readonly IActionValidator _actionValidator = actionValidator;
    private readonly IBuilderContext _builderContext = builderContext;
    private readonly IRqlSettings _settings = settings;

    protected override Result<bool> ValidatePath(RqlPropertyInfo property, string path)
    {
        if (!_actionValidator.Validate(property, RqlActions.Filter))
            return Error.Validation("Filtering is not permitted.", _builderContext.GetFullPath(path));
        return true;
    }

    protected override bool UseSafeNavigation() => _settings.Filter.Navigation == NavigationStrategy.Safe;
}
