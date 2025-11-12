using Mpt.Rql.Abstractions.Configuration;
using Mpt.Rql.Core;
using Mpt.Rql.Services.Filtering.Operators.Collection;
using System.Reflection;

namespace Mpt.Rql.Services.Filtering.Operators.Collection.Implementation;

internal class Any(IRqlSettings settings) : CollectionOperator(settings), IAny
{
    protected override RqlOperators Operator => RqlOperators.Any;

    protected override Result<MethodInfo> GetFunction(ICollectionFunctions factory, bool noPredicate)
        => noPredicate ? factory.GetAnyWithNoPredicate() : factory.GetAnyWithPredicate();
}
