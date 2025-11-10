using Mpt.Rql.Abstractions;
using Mpt.Rql.Linq.Core.Result;
using Mpt.Rql.Linq.Services.Filtering.Operators.List;
using System.Linq.Expressions;

namespace Rql.Tests.Integration.Tests.Extensibility.Utility;

internal class CustomListHandler : IListIn
{
    public Result<Expression> MakeExpression(IRqlPropertyInfo propertyInfo, MemberExpression member, IEnumerable<string> list)
    {
        throw new NotImplementedException();
    }
}
