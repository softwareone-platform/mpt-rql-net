#pragma warning disable IDE0130
namespace SoftwareOne.Rql.Client;

internal interface IComparableOperator
{
    QueryOperator ToQueryOperator();
};