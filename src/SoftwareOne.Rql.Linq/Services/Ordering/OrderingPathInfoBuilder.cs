using ErrorOr;
using SoftwareOne.Rql.Linq.Core;
using SoftwareOne.Rql.Linq.Core.Metadata;

namespace SoftwareOne.Rql.Linq.Services.Filtering
{
    internal interface IOrderingPathInfoBuilder : IPathInfoBuilder { }

    internal class OrderingPathInfoBuilder : PathInfoBuilder, IOrderingPathInfoBuilder
    {
        public OrderingPathInfoBuilder(IMetadataProvider metadataProvider) : base(metadataProvider)
        {
        }

        protected override ErrorOr<Success> ValidatePath(MemberPathInfo pathInfo)
        {
            if (!pathInfo.PropertyInfo.Actions.HasFlag(RqlActions.Order))
                return Error.Validation(pathInfo.Path.ToString(), "Ordering is not permitted.");
            return Result.Success;
        }
    }
}
