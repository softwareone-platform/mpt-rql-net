using SoftwareOne.Rql.Client;

namespace SoftwareOne.Rql.Linq.Client.Dsl;

internal interface IComparableOperator
{
    QueryOperator ToQueryOperator(IPropertyVisitor propertyVisitor);
};