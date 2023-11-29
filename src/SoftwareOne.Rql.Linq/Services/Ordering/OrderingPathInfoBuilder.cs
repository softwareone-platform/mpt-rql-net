using ErrorOr;
using SoftwareOne.Rql.Linq.Core;
using SoftwareOne.Rql.Linq.Core.Metadata;

namespace SoftwareOne.Rql.Linq.Services.Ordering
{
    internal interface IOrderingPathInfoBuilder : IPathInfoBuilder { }

    internal class OrderingPathInfoBuilder : PathInfoBuilder, IOrderingPathInfoBuilder
    {
        private readonly IActionValidator _actionValidator;

        public OrderingPathInfoBuilder(IActionValidator actionValidator, IMetadataProvider metadataProvider) : base(metadataProvider)
        {
            _actionValidator = actionValidator;
        }

        protected override ErrorOr<Success> ValidatePath(MemberPathInfo pathInfo)
        {
            if (!_actionValidator.Validate(pathInfo.PropertyInfo, RqlActions.Order))
                return Error.Validation(pathInfo.Path.ToString(), "Ordering is not permitted.");
            return Result.Success;
        }
    }
}
