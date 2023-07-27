using ErrorOr;
using System.Linq.Expressions;

namespace SoftwareOne.Rql.Linq.Services.Filtering.Operators.Comparison.Implementation
{
    internal class ComparisonOperator
    {
        protected ErrorOr<Expression> MakeBinaryExpression(MemberExpression member, string? value, Func<Expression, Expression, BinaryExpression> method)
        {
            if (value == null)
            {
                // check if member type is nullable
                if (member.Type.IsValueType && Nullable.GetUnderlyingType(member.Type) == null)
                    return Error.Validation(description: $"Cannot compare non nullable property with null");

                return method(member, Expression.Constant(null, member.Type));
            }

            var eoT = ConstantHelper.ChangeType(value, member.Type);
            if (eoT.IsError)
                return eoT.Errors;

            ConstantExpression constant = Expression.Constant(eoT.Value, member.Type);

            return method(member, constant);
        }
    }
}
