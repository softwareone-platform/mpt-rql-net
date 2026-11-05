namespace Mpt.Rql.Abstractions.Exception;

public class RqlInvalidDependencyException : System.Exception
{
    public RqlInvalidDependencyException(string message)
        : base(message)
    {
    }
}
