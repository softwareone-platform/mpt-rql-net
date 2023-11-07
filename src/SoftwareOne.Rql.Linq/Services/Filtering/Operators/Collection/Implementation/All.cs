using ErrorOr;
using System.Reflection;

namespace SoftwareOne.Rql.Linq.Services.Filtering.Operators.Collection.Implementation;

internal class All : CollectionOperator, IAll
{
    protected override RqlOperators Operator => RqlOperators.Any;

    protected override ErrorOr<MethodInfo> GetFunction(ICollectionFunctions factory, bool noPredicate)
    {
        if (noPredicate)
            return Error.Validation(description: "all() function must have 2 arguments");
        return factory.GetAll();
    }
}