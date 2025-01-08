using System.Linq.Expressions;

#pragma warning disable IDE0130
namespace SoftwareOne.Rql;

public class RqlMapProperty
{
    public string TargetPath { get; internal set; } = null!;

    public LambdaExpression SourceExpression { get; internal set; } = null!;

    public bool IsDynamic { get; internal set; }
}