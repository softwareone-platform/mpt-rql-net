using SoftwareOne.Rql.Abstractions;
using SoftwareOne.Rql.Linq.Core.Expressions;
using System.Linq.Expressions;
using System.Reflection;
using SoftwareOne.Rql.Linq.Core.Result;

namespace SoftwareOne.Rql.Linq.Services.Filtering.Operators.Comparison.Implementation
{
    internal abstract class ComparisonOperator : IComparisonOperator
    {
        private static readonly MethodInfo _stringOperator = typeof(string).GetMethod(nameof(string.Compare), BindingFlags.Public | BindingFlags.Static, [typeof(string), typeof(string)])!;

        public Result<Expression> MakeExpression(IRqlPropertyInfo propertyInfo, Expression accessor, string? value)
        => MakeBinaryExpression(propertyInfo, accessor, value);

        protected Result<Expression> MakeBinaryExpression(IRqlPropertyInfo propertyInfo, Expression accessor, string? value)
        {
            var validationResult = ValidationHelper.ValidateOperatorApplicability(propertyInfo, Operator);

            if (validationResult.IsError)
                return validationResult.Errors;

            if (accessor.Type == typeof(string))
            {
                return Handler(Expression.Call(_stringOperator, accessor, ConstantBuilder.Build(value, typeof(string))), Expression.Constant(0, typeof(int)));
            }
            else if (value == null)
            {
                // check if member type is nullable
                if (accessor.Type.IsValueType && Nullable.GetUnderlyingType(accessor.Type) == null)
                    return Error.Validation("Cannot compare non nullable property with null");

                return Handler(accessor, ConstantBuilder.Build(null, accessor.Type));
            }

            var converted = ConstantHelper.ChangeType(value, accessor.Type);

            if (converted.IsError)
                return converted.Errors;

            var constant = ConstantBuilder.Build(converted.Value, accessor.Type);

            return Handler(accessor, constant);
        }

        protected abstract RqlOperators Operator { get; }

        internal abstract Func<Expression, Expression, BinaryExpression> Handler { get; }
    }
}
