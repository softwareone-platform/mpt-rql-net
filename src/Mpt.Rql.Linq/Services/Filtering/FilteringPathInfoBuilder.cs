using Mpt.Rql.Abstractions.Result;
using Mpt.Rql.Linq.Core;
using Mpt.Rql.Linq.Core.Metadata;
using Mpt.Rql.Linq.Core.Result;
using Mpt.Rql.Linq.Services.Context;

namespace Mpt.Rql.Linq.Services.Filtering;

internal interface IFilteringPathInfoBuilder : IPathInfoBuilder { }

internal class FilteringPathInfoBuilder : PathInfoBuilder, IFilteringPathInfoBuilder
{
    private readonly IActionValidator _actionValidator;
    private readonly IBuilderContext _builderContext;

    public FilteringPathInfoBuilder(IActionValidator actionValidator, IMetadataProvider metadataProvider, IBuilderContext builderContext) : base(metadataProvider, builderContext)
    {
        _actionValidator = actionValidator;
        _builderContext = builderContext;
    }

    protected override Result<bool> ValidatePath(MemberPathInfo pathInfo)
    {
        if (!_actionValidator.Validate(pathInfo.PropertyInfo, RqlActions.Filter))
            return Error.Validation("Filtering is not permitted.", _builderContext.GetFullPath(pathInfo.Path.ToString()));
        return true;
    }
}
