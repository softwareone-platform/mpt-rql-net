using SoftwareOne.Rql.Linq.Core.Result;
using System.Reflection;

namespace SoftwareOne.Rql.Linq.Services.Filtering.Operators.Collection.Implementation;

internal class Any : CollectionOperator, IAny
{
    protected override RqlOperators Operator => RqlOperators.Any;

    protected override Result<MethodInfo> GetFunction(ICollectionFunctions factory, bool noPredicate)
        => noPredicate ? factory.GetAnyWithNoPredicate() : factory.GetAnyWithPredicate();
}
