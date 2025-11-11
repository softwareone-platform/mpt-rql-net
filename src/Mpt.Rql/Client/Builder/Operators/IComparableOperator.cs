using Mpt.Rql.Client;
using Mpt.Rql.Linq.Client.Builder.Operators;

namespace Mpt.Rql.Client.Builder.Operators;

internal interface IComparableOperator
{
    QueryOperator ToQueryOperator(IPropertyVisitor propertyVisitor);
};