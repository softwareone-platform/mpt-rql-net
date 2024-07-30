using SoftwareOne.Rql.Linq.Core;
using SoftwareOne.Rql.Linq.Core.Metadata;
using SoftwareOne.Rql.Linq.Core.Result;
using SoftwareOne.Rql.Linq.Services.Context;

namespace SoftwareOne.Rql.Linq.Services.Ordering
{
    internal interface IOrderingPathInfoBuilder : IPathInfoBuilder { }

    internal class OrderingPathInfoBuilder : PathInfoBuilder, IOrderingPathInfoBuilder
    {
        private readonly IActionValidator _actionValidator;
        private readonly IBuilderContext _builderContext;

        public OrderingPathInfoBuilder(IActionValidator actionValidator, IMetadataProvider metadataProvider, IBuilderContext builderContext) : base(metadataProvider, builderContext)
        {
            _actionValidator = actionValidator;
            _builderContext = builderContext;
        }

        protected override Result<bool> ValidatePath(MemberPathInfo pathInfo)
        {
            if (!_actionValidator.Validate(pathInfo.PropertyInfo, RqlActions.Order))
                return Error.Validation("Ordering is not permitted.", _builderContext.GetFullPath(pathInfo.Path.ToString()));
            return true;
        }
    }
}
