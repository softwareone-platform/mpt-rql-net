namespace SoftwareOne.Rql.Abstractions.Exception;

public class RqlParserException : System.Exception
{
    public RqlParserException(string message)
        : base(message)
    {
    }
}