using Mpt.Rql;

namespace Mpt.UnitTests.Common;

internal static class ErrorMessageFactory
{
    internal static string OperatorProhibited(RqlOperators rqlOperator) => $"Operator '{rqlOperator}' is not permitted";
}
