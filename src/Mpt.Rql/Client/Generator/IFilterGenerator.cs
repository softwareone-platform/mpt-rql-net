namespace Mpt.Rql.Client.Generator;

internal interface IFilterGenerator
{
    string? Generate(IOperator? filterOperator);
}