using System.Reflection;

namespace SoftwareOne.Rql.Linq.Services.Filtering.Operators.Collection.Implementation;

internal class All : CollectionOperator, IAll
{
    protected override RqlOperators Operator => RqlOperators.Any;

    protected override MethodInfo GetFunction(ICollectionFunctions factory)
        => factory.GetAll();
}