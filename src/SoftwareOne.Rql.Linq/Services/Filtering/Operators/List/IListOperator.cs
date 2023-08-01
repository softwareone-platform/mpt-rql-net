using ErrorOr;
using System.Linq.Expressions;

namespace SoftwareOne.Rql.Linq.Services.Filtering.Operators.List
{
    public interface IListOperator : IOperator
    {
        ErrorOr<Expression> MakeExpression(MemberExpression member, IEnumerable<string> list);
    }
}
