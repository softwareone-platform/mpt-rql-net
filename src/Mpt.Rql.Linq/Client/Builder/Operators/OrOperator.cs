using Mpt.Rql.Client;

namespace Mpt.Rql.Linq.Client.Builder.Operators;

internal record OrOperator(params IOperator[] Operators) : IOperator;