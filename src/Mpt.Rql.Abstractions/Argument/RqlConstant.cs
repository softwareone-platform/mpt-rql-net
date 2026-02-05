namespace Mpt.Rql.Abstractions.Argument;

public class RqlConstant : RqlArgument
{
    private readonly string _value;
    private readonly bool _isQuoted;

    internal RqlConstant(string value, bool isQuoted = false)
    {
        _value = value;
        _isQuoted = isQuoted;
    }

    public string Value => _value;
    public bool IsQuoted => _isQuoted;
}
