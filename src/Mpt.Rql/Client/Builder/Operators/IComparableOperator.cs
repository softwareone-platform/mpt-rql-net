namespace Mpt.Rql.Linq.Client.Builder.Operators;

internal interface IComparableOperator
{
    QueryOperator ToQueryOperator(IPropertyVisitor propertyVisitor);
};