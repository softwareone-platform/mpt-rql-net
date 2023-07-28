using SoftwareOne.Rql.Abstractions;

namespace SoftwareOne.Rql.Parsers.Linear
{
    internal record struct ExpressionPair(GroupType Type, RqlExpression Expression);
}
