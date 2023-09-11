using SoftwareOne.Rql.Client;

namespace SoftwareOne.Rql.Linq.Client.Builder.Operators;

internal record AndOperator(params IOperator[] Operators) : IOperator;