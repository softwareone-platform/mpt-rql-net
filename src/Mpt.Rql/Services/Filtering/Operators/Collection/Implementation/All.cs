using Mpt.Rql.Abstractions.Configuration;
using Mpt.Rql.Abstractions.Result;
using Mpt.Rql.Core;
using System.Reflection;

namespace Mpt.Rql.Services.Filtering.Operators.Collection.Implementation;

internal class All(IRqlSettings settings) : CollectionOperator(settings), IAll
{
    protected override RqlOperators Operator => RqlOperators.Any;

    protected override Result<MethodInfo> GetFunction(ICollectionFunctions factory, bool noPredicate)
    {
        if (noPredicate)
            return Error.Validation("all() function must have 2 arguments");
        return factory.GetAll();
    }
}