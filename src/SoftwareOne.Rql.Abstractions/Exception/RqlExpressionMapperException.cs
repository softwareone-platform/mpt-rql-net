namespace SoftwareOne.Rql.Abstractions.Exception;

public class RqlExpressionMapperException : System.Exception
{
    public RqlExpressionMapperException()
    {
    }

    public RqlExpressionMapperException(string message)
        : base(message)
    {
    }

    public RqlExpressionMapperException(string message, System.Exception inner)
        : base(message, inner)
    {
    }
}