using Mpt.Rql.Abstractions;
using Mpt.Rql.Abstractions.Configuration;
using Mpt.Rql.Abstractions.Configuration.Filter;
using Mpt.Rql.Abstractions.Result;
using Mpt.Rql.Core;
using Mpt.Rql.Core.Expressions;
using System.Linq.Expressions;
using System.Reflection;

namespace Mpt.Rql.Services.Filtering.Operators.Comparison.Implementation;

internal abstract class ComparisonOperator(IRqlSettings settings) : IComparisonOperator
{
    private static readonly MethodInfo _stringOperator = typeof(string).GetMethod(nameof(string.Compare), BindingFlags.Public | BindingFlags.Static, [typeof(string), typeof(string)])!;

    public Result<Expression> MakeExpression(IRqlPropertyInfo propertyInfo, Expression accessor, string? value)
    {
        var validationResult = ValidationHelper.ValidateOperatorApplicability(propertyInfo, Operator);

        if (validationResult.IsError)
            return validationResult.Errors;

        return MakeBinaryExpression(accessor, value);
    }

    protected virtual Result<Expression> MakeBinaryExpression(Expression accessor, string? value)
    {
        if (accessor.Type == typeof(string) && settings.Filter.Strings.Strategy == StringComparisonStrategy.Lexicographical && !AvoidLexicographicalComparison)
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

        var binaryExpression = Handler(accessor, constant);

        return binaryExpression.WithNullSafetyIfEnabled(accessor, settings);
    }

    protected virtual bool AvoidLexicographicalComparison { get; } = false;

    protected abstract RqlOperators Operator { get; }

    internal abstract Func<Expression, Expression, BinaryExpression> Handler { get; }
}
