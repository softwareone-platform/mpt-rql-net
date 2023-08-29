using SoftwareOne.Rql.Client;

namespace SoftwareOne.Rql.Linq.Client.Dsl;

internal record OrOperator(IOperator Left, IOperator Right) : Operator;