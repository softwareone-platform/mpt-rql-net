namespace Mpt.Rql.Abstractions.Argument.Pointer;

/// <summary>
/// A member path applied to the result of another expression: <c>first(parameters,eq(name,"priority")).value</c>.
/// <see cref="RqlPointer.Inner"/> is the evaluated expression (never <c>null</c>), <see cref="Path"/> the dotted
/// path read from its result.
/// </summary>
public class RqlMemberAccess : RqlPointer
{
    internal RqlMemberAccess(RqlExpression inner, string path) : base(inner)
    {
        Path = path;
    }

    public string Path { get; }
}
