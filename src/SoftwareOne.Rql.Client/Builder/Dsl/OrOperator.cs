namespace SoftwareOne.Rql.Client.Builder.Dsl;

public record OrOperator(IOperator Left, IOperator Right) : Operator;