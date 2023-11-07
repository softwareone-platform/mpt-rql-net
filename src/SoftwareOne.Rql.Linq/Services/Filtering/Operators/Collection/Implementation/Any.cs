using ErrorOr;
using System.Reflection;

namespace SoftwareOne.Rql.Linq.Services.Filtering.Operators.Collection.Implementation;

internal class Any : CollectionOperator, IAny
{
    protected override RqlOperators Operator => RqlOperators.Any;

    protected override ErrorOr<MethodInfo> GetFunction(ICollectionFunctions factory, bool noPredicate)
        => noPredicate ? factory.GetAnyWithNoPredicate() : factory.GetAnyWithPredicate();
}
