namespace SoftwareOne.Rql.Linq.Client.Builder.Dsl;

public record AndOperator(IOperator Left, IOperator Right) : Operator;