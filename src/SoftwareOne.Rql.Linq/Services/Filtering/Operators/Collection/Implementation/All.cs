using SoftwareOne.Rql.Linq.Core.Result;
using System.Reflection;

namespace SoftwareOne.Rql.Linq.Services.Filtering.Operators.Collection.Implementation;

internal class All : CollectionOperator, IAll
{
    protected override RqlOperators Operator => RqlOperators.Any;

    protected override Result<MethodInfo> GetFunction(ICollectionFunctions factory, bool noPredicate)
    {
        if (noPredicate)
            return Error.Validation("all() function must have 2 arguments");
        return factory.GetAll();
    }
}