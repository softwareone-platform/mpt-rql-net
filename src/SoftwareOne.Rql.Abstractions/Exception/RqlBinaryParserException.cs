namespace SoftwareOne.Rql.Abstractions.Exception;

public class RqlBinaryParserException : System.Exception
{
    public RqlBinaryParserException()
    {
    }

    public RqlBinaryParserException(string message)
        : base(message)
    {
    }

    public RqlBinaryParserException(string message, System.Exception inner)
        : base(message, inner)
    {
    }
}