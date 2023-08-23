namespace SoftwareOne.Rql.Abstractions.Exception;

public class RqlUnaryParserException : System.Exception
{
    public RqlUnaryParserException()
    {
    }

    public RqlUnaryParserException(string message)
        : base(message)
    {
    }

    public RqlUnaryParserException(string message, System.Exception inner)
        : base(message, inner)
    {
    }
}