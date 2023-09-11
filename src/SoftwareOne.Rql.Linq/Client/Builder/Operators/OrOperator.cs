using SoftwareOne.Rql.Client;

namespace SoftwareOne.Rql.Linq.Client.Builder.Operators;

internal record OrOperator(params IOperator[] Operators) : IOperator;