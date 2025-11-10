using Mpt.Rql.Abstractions;
using Mpt.Rql.Linq.Core;
using Mpt.Rql.Linq.Services.Filtering.Operators.Comparison;
using System.Linq.Expressions;

namespace Rql.Tests.Integration.Tests.Extensibility.Utility;

internal class CustomEqualHandler : IEqual
{
    public Result<Expression> MakeExpression(IRqlPropertyInfo propertyInfo, Expression member, string? value)
    {
        throw new NotImplementedException();
    }
}
