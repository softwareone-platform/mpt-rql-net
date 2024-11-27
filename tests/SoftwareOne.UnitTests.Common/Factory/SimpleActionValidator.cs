using SoftwareOne.Rql;
using SoftwareOne.Rql.Linq.Core;

namespace SoftwareOne.UnitTests.Common;

internal class SimpleActionValidator : IActionValidator
{
    public bool Validate(RqlPropertyInfo propertyInfo, RqlActions action)
        => propertyInfo.Actions.HasFlag(action);
}
