using SoftwareOne.Rql.Linq.Client.Core;

namespace SoftwareOne.Rql.Linq.Client.Builder.Operators;

internal interface IComparableOperator
{
    QueryOperator ToQueryOperator(IPropertyVisitor propertyVisitor);
};