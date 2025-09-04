using SoftwareOne.Rql.Abstractions;
using SoftwareOne.Rql.Abstractions.Result;

namespace SoftwareOne.Rql.Linq.Services.Filtering.Operators;
internal static class ValidationHelper
{
    public static Result<bool> ValidateOperatorApplicability(IRqlPropertyInfo propertyInfo, RqlOperators rqlOperator)
    {
        if (!propertyInfo.Operators.HasFlag(rqlOperator))
            return Error.Validation($"Operator '{rqlOperator}' is not permitted");

        return true;
    }
}
