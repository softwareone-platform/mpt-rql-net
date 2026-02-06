namespace Mpt.Rql.Client.Builder.Operators;

internal record OrOperator(params IOperator[] Operators) : IOperator;