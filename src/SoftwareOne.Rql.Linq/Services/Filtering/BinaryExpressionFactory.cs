using ErrorOr;
using SoftwareOne.Rql.Abstractions;
using SoftwareOne.Rql.Abstractions.Binary;
using SoftwareOne.Rql.Abstractions.Constant;
using SoftwareOne.Rql.Abstractions.Group;
using SoftwareOne.Rql.Linq.Services.Filtering.Operators.List;
using System.Linq.Expressions;

namespace SoftwareOne.Rql.Linq.Services.Filtering;

internal static class BinaryExpressionFactory
{
    internal static ErrorOr<Expression> MakeSimple(RqlBinary node, bool allowNull, Func<string?, ErrorOr<Expression>> factory)
    {
        var arg = GetRightArgument(node.Right, allowNull);

        return arg.IsError ? arg.Errors : factory(arg.Value);
    }

    internal static ErrorOr<Expression> MakeList(RqlBinary node, IRqlPropertyInfo propertyInfo, MemberExpression member, IListOperator list)
    {
        if (node.Right is not RqlGroup grp || grp.Items == null || grp.Items.Count == 0)
            return Error.Validation(description: "Value has to be a non empty array.");

        var values = grp.Items.Select(x => GetRightArgument(x, false)).ToList();

        if (values.Exists(t => t.IsError))
            return values.SelectMany(s => s.Errors).ToList();

        return list.MakeExpression(propertyInfo, member, values.Select(s => s.Value!));
    }

    private static ErrorOr<string?> GetRightArgument(RqlExpression right, bool allowNull)
    {
        ErrorOr<string?> res = right switch
        {
            RqlNull => (string?)null,
            RqlEmpty => string.Empty,
            RqlConstant str => str.Value,
            _ => Error.Validation(description: "Unsupported argument type.")
        };

        if (!allowNull && !res.IsError && res.Value == null)
            return Error.Validation(description: "Null values are not supported.");

        return res;
    }
}