namespace SoftwareOne.Rql.Linq.Client.Builder.Dsl;

public record OrOperator(IOperator Left, IOperator Right) : Operator;