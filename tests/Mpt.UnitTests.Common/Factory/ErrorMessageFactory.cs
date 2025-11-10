using Mpt.Rql;

namespace Mpt.UnitTests.Common.Factory;

internal static class ErrorMessageFactory
{
    internal static string OperatorProhibited(RqlOperators rqlOperator) => $"Operator '{rqlOperator}' is not permitted";
}
