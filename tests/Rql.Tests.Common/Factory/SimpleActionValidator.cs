using Mpt.Rql;
using Mpt.Rql.Core;

namespace Rql.Tests.Common.Factory;

internal class SimpleActionValidator : IActionValidator
{
    public bool Validate(RqlPropertyInfo propertyInfo, RqlActions action)
        => propertyInfo.Actions.HasFlag(action);
}
