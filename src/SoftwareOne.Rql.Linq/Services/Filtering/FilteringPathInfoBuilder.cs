using ErrorOr;
using SoftwareOne.Rql.Linq.Core;
using SoftwareOne.Rql.Linq.Core.Metadata;

namespace SoftwareOne.Rql.Linq.Services.Filtering
{
    internal interface IFilteringPathInfoBuilder : IPathInfoBuilder { }

    internal class FilteringPathInfoBuilder : PathInfoBuilder, IFilteringPathInfoBuilder
    {
        private readonly IActionValidator _actionValidator;

        public FilteringPathInfoBuilder(IActionValidator actionValidator, IMetadataProvider metadataProvider) : base(metadataProvider)
        {
            _actionValidator = actionValidator;
        }

        protected override ErrorOr<Success> ValidatePath(MemberPathInfo pathInfo)
        {
            if (!_actionValidator.Validate(pathInfo.PropertyInfo, RqlActions.Filter))
                return Error.Validation(description: "Filtering is not permitted.");
            return Result.Success;
        }
    }
}
