using SoftwareOne.Rql.Linq.Core;
using SoftwareOne.Rql.Linq.Core.Metadata;
using SoftwareOne.Rql.Linq.Core.Result;

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

        protected override Result<bool> ValidatePath(MemberPathInfo pathInfo)
        {
            if (!_actionValidator.Validate(pathInfo.PropertyInfo, RqlActions.Order))
                return Error.Validation("Ordering is not permitted.", pathInfo.Path.ToString());
            return true;
        }
    }
}
