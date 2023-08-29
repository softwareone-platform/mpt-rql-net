using ErrorOr;
using SoftwareOne.Rql.Abstractions;
using System.Linq.Expressions;

namespace SoftwareOne.Rql.Linq.Services.Filtering.Operators.Comparison.Implementation
{
    internal abstract class ComparisonOperator
    {
        protected ErrorOr<Expression> MakeBinaryExpression(IRqlPropertyInfo propertyInfo, MemberExpression member, string? value, Func<Expression, Expression, BinaryExpression> method)
        {
            var validationResult = ValidationHelper.ValidateOperatorApplicability(propertyInfo, Operator);
            if (validationResult.IsError)
                return validationResult.Errors;

            if (value == null)
            {
                // check if member type is nullable
                if (member.Type.IsValueType && Nullable.GetUnderlyingType(member.Type) == null)
                    return Error.Validation(description: $"Cannot compare non nullable property with null");

                return method(member, Expression.Constant(null, member.Type));
            }

            var converted = ConstantHelper.ChangeType(value, member.Type);
            if (converted.IsError)
                return converted.Errors;

            ConstantExpression constant = Expression.Constant(converted.Value, member.Type);

            return method(member, constant);
        }

        protected abstract RqlOperators Operator { get; }
    }
}
