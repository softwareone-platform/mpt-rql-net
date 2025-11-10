namespace SoftwareOne.Rql.Abstractions.Argument.Pointer;

public abstract class RqlPointer : RqlArgument
{
    private readonly RqlExpression? _inner;

    private protected RqlPointer(RqlExpression? inner = null)
    {
        _inner = inner;
    }

    public RqlExpression? Inner => _inner;
}
