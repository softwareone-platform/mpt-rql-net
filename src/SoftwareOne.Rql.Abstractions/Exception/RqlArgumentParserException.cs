namespace SoftwareOne.Rql.Abstractions.Exception;

public class RqlArgumentParserException : System.Exception
{
    public RqlArgumentParserException(string message)
        : base(message)
    {
    }
}