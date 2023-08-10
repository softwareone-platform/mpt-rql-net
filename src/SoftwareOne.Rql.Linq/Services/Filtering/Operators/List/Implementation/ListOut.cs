using ErrorOr;
using System.Linq.Expressions;

namespace SoftwareOne.Rql.Linq.Services.Filtering.Operators.List.Implementation;

internal class ListOut : ListIn, IListOut
{
    public override ErrorOr<Expression> MakeExpression(MemberExpression member, IEnumerable<string> list)
    {
        var eoExp = base.MakeExpression(member, list);

        return eoExp.IsError ? eoExp : Expression.Not(eoExp.Value);
    }
}