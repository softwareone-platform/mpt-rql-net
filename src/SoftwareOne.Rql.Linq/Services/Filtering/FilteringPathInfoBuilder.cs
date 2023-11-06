using ErrorOr;
using SoftwareOne.Rql.Linq.Core;
using SoftwareOne.Rql.Linq.Core.Metadata;

namespace SoftwareOne.Rql.Linq.Services.Filtering
{
    internal interface IFilteringPathInfoBuilder : IPathInfoBuilder { }

    internal class FilteringPathInfoBuilder : PathInfoBuilder, IFilteringPathInfoBuilder
    {
        public FilteringPathInfoBuilder(IMetadataProvider metadataProvider) : base(metadataProvider)
        {
        }

        protected override ErrorOr<Success> ValidatePath(MemberPathInfo pathInfo)
        {
            if (!pathInfo.PropertyInfo.Actions.HasFlag(RqlActions.Filter))
                return Error.Validation(description: "Filtering is not permitted");
            return Result.Success;
        }
    }
}
