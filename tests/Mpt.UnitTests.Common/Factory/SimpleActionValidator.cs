using Mpt.Rql;
using Mpt.Rql.Linq.Core;

namespace Mpt.UnitTests.Common.Factory;

internal class SimpleActionValidator : IActionValidator
{
    public bool Validate(RqlPropertyInfo propertyInfo, RqlActions action)
        => propertyInfo.Actions.HasFlag(action);
}
