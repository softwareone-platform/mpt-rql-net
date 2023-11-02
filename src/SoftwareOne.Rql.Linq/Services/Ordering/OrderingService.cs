using ErrorOr;
using SoftwareOne.Rql.Abstractions;
using SoftwareOne.Rql.Abstractions.Argument;
using SoftwareOne.Rql.Linq.Core;
using SoftwareOne.Rql.Linq.Core.Metadata;
using System.Linq.Expressions;
using System.Reflection;

namespace SoftwareOne.Rql.Linq.Services.Ordering;

internal sealed class OrderingService<TView> : RqlService, IOrderingService<TView>
{
    private readonly IRqlParser _parser;

    public OrderingService(IMetadataProvider typeMetadataProvider, IRqlParser parser) : base(typeMetadataProvider)
    {
        _parser = parser;
    }

    protected override string ErrorPrefix => "order";

    public ErrorOr<IQueryable<TView>> Apply(IQueryable<TView> query, string? order)
    {
        if (string.IsNullOrEmpty(order))
            return ErrorOrFactory.From(query);

        var node = _parser.Parse(order);

        var orderProperties = node.Items!.OfType<RqlConstant>().ToList();
        if (!orderProperties.Any())
            return Error.Validation(MakeErrorCode("no_props"), "No valid ordering properties were detected");

        var isFirst = true;
        var param = Expression.Parameter(typeof(TView));

        var errors = new List<Error>();

        foreach (var property in orderProperties)
        {
            var (path, isAsc) = StringHelper.ExtractSign(property.Value);
            var member = MakeOrderingMemberAccess(param, path.ToString());

            if (member.IsError)
            {
                errors.AddRange(member.Errors);
                continue;
            }

            var method = MakeOrderingMethod(member.Value.Expression, isAsc, isFirst);
            var expression = Expression.Lambda(member.Value.Expression, param);

            query = (IQueryable<TView>)method.Invoke(null, new object[] { query, expression })!;
            isFirst = false;
        }

        return errors.Any() ? errors : ErrorOrFactory.From(query);
    }

    private ErrorOr<MemberPathInfo> MakeOrderingMemberAccess(ParameterExpression param, string path)
    {
        return MakeMemberAccess(param, path.ToString(), path =>
        {
            if (!path.PropertyInfo.Actions.HasFlag(RqlActions.Order))
                return Error.Validation(MakeErrorCode(path.Path.ToString()), "Ordering is not permitted.");
            return Result.Success;
        });
    }

    private static MethodInfo MakeOrderingMethod(Expression member, bool isAsc, bool isFirst)
    {
        var functions = (IOrderingFunctions)Activator.CreateInstance(typeof(OrderingFunctions<,>).MakeGenericType(typeof(TView), member.Type))!;

        if (isAsc)
            return isFirst ? functions.GetOrderBy() : functions.GetThenBy();
        else
            return isFirst ? functions.GetOrderByDescending() : functions.GetThenByDescending();
    }
}