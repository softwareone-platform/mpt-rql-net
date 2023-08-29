using SoftwareOne.Rql.Client;

namespace SoftwareOne.Rql.Linq.Client.Dsl;

internal record AndOperator(IOperator Left, IOperator Right) : Operator;