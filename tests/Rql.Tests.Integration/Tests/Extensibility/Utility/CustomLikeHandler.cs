using Mpt.Rql.Abstractions;
using Mpt.Rql.Linq.Core.Result;
using Mpt.Rql.Linq.Services.Filtering.Operators.Search;
using System.Linq.Expressions;

namespace Rql.Tests.Integration.Tests.Extensibility.Utility;

internal class CustomLikeHandler : ILike
{
    public Result<Expression> MakeExpression(IRqlPropertyInfo propertyInfo, MemberExpression member, string? value)
    {
        throw new NotImplementedException();
    }
}
