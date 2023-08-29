using SoftwareOne.Rql;

namespace Rql.Tests.Unit.Factory;

internal static class ErrorMessageFactory
{
    internal static string OperatorProhibited(RqlOperators rqlOperator) => $"Operator '{rqlOperator}' is not permitted";
}

