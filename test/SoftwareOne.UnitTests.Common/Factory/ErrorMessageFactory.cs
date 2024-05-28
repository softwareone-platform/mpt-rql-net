using SoftwareOne.Rql;

namespace SoftwareOne.UnitTests.Common;

internal static class ErrorMessageFactory
{
    internal static string OperatorProhibited(RqlOperators rqlOperator) => $"Operator '{rqlOperator}' is not permitted";
}
