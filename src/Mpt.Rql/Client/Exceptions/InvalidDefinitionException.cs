#pragma warning disable IDE0130
namespace Mpt.Rql.Client;

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