using ErrorOr;
using SoftwareOne.Rql.Abstractions;

namespace SoftwareOne.Rql.Linq.Services.Filtering.Operators;
internal static class ValidationHelper
{
    public static ErrorOr<Success> ValidateOperatorApplicability(IRqlPropertyInfo propertyInfo, RqlOperators rqlOperator)
    {
        if (!propertyInfo.Operators.HasFlag(rqlOperator))
            return Error.Validation(description: $"Operator '{rqlOperator}' is not permitted");

        return Result.Success;
    }
}
