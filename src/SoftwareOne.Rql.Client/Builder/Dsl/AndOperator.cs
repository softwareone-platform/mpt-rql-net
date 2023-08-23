namespace SoftwareOne.Rql.Client.Builder.Dsl;

public record AndOperator(IOperator Left, IOperator Right) : Operator;