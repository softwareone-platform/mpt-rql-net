using ErrorOr;
using SoftwareOne.Rql.Abstractions;
using SoftwareOne.Rql.Linq.Services.Filtering.Operators.Comparison;
using System.Linq.Expressions;

namespace Rql.Tests.Integration.Tests.Extensibility.Utility
{
    internal class CustomEqualHandler : IEqual
    {
        public ErrorOr<Expression> MakeExpression(IRqlPropertyInfo propertyInfo, MemberExpression member, string? value)
        {
            throw new NotImplementedException();
        }
    }
}
