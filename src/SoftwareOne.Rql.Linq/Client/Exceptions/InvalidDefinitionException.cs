#pragma warning disable IDE0130
namespace SoftwareOne.Rql.Client.Exceptions;

public class InvalidDefinitionException : Exception
{
    public InvalidDefinitionException()
    {
    }

    public InvalidDefinitionException(string message) : base(message)
    {
    }

    public InvalidDefinitionException(string message, Exception inner) : base(message, inner)
    {
    }
}