using Mpt.Rql.Abstractions;
using Mpt.Rql.Core;
using Mpt.Rql.Services.Filtering.Operators.Search;
using System.Linq.Expressions;

namespace Rql.Tests.Integration.Tests.Extensibility.Utility;

internal class CustomLikeHandler : ILike
{
    public Result<Expression> MakeExpression(IRqlPropertyInfo propertyInfo, Expression accessor, string? value)
    {
        throw new NotImplementedException();
    }
}
