using SoftwareOne.Rql.Abstractions;
using SoftwareOne.Rql.Linq.Core.Result;
using SoftwareOne.Rql.Linq.Services.Filtering.Operators.List;
using System.Linq.Expressions;

namespace Rql.Tests.Integration.Tests.Extensibility.Utility
{
    internal class CustomListHandler : IListIn
    {
        public Result<Expression> MakeExpression(IRqlPropertyInfo propertyInfo, MemberExpression member, IEnumerable<string> list)
        {
            throw new NotImplementedException();
        }
    }
}
