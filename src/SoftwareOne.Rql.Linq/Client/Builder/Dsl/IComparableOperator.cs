using SoftwareOne.Rql.Client;

namespace SoftwareOne.Rql.Linq.Client.Builder.Dsl;

internal interface IComparableOperator
{
    QueryOperator ToQueryOperator();
};