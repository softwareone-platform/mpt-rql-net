using Mpt.Rql.Abstractions;
using Mpt.Rql.Abstractions.Result;
using Mpt.Rql.Core;

namespace Mpt.Rql.Services.Filtering.Operators;

internal static class ValidationHelper
{
    public static Result<bool> ValidateOperatorApplicability(IRqlPropertyInfo propertyInfo, RqlOperators rqlOperator)
    {
        if (!propertyInfo.Operators.HasFlag(rqlOperator))
            return Error.Validation($"Operator '{rqlOperator}' is not permitted");

        return true;
    }
}
