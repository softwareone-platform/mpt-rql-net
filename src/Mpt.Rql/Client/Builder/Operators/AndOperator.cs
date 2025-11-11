using Mpt.Rql.Client;

namespace Mpt.Rql.Linq.Client.Builder.Operators;

internal record AndOperator(params IOperator[] Operators) : IOperator;