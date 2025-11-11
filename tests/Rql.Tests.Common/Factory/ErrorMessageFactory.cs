using Mpt.Rql;

namespace Rql.Tests.Common.Factory;

internal static class ErrorMessageFactory
{
    internal static string OperatorProhibited(RqlOperators rqlOperator) => $"Operator '{rqlOperator}' is not permitted";
}
