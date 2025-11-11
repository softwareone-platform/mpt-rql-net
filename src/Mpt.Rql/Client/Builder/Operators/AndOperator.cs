using Mpt.Rql.Client;

namespace Mpt.Rql.Client.Builder.Operators;

internal record AndOperator(params IOperator[] Operators) : IOperator;