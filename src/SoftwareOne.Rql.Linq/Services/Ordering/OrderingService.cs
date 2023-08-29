using ErrorOr;
using SoftwareOne.Rql.Abstractions;
using SoftwareOne.Rql.Abstractions.Constant;
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

        var orderProps = node.Items!.OfType<RqlConstant>().ToList();
        if (!orderProps.Any())
            return Error.Validation(MakeErrorCode("no_props"), "No valid ordering properties were detected");

        var isFirst = true;
        var param = Expression.Parameter(typeof(TView));

        var errors = new List<Error>();

        foreach (var op in orderProps)
        {
            var (path, isAsc) = StringHelper.ExtractSign(op.Value);

                var memberInfo = MakeMemberAccess(param, path.ToString(), path =>
                {
                    if (!path.PropertyInfo.Actions.HasFlag(RqlActions.Order))
                        return Error.Validation(MakeErrorCode(path.Path.ToString()), "Ordering is not permitted.");
                    return Result.Success;
                });

            if (memberInfo.IsError)
            {
                errors.AddRange(memberInfo.Errors);
                continue;
            }

            var member = memberInfo.Value.Expression;

            var functions = (IOrderingFunctions)Activator.CreateInstance(typeof(OrderingFunctions<,>).MakeGenericType(typeof(TView), member.Type))!;

            MethodInfo methodInfo;
            if (isAsc)
                methodInfo = isFirst ? functions.GetOrderBy() : functions.GetThenBy();
            else
                methodInfo = isFirst ? functions.GetOrderByDescending() : functions.GetThenByDescending();

            var orderExp = Expression.Lambda(member, param);

            query = (IQueryable<TView>)methodInfo.Invoke(null, new object[] { query, orderExp })!;
            isFirst = false;
        }

        return errors.Any() ? errors : ErrorOrFactory.From(query);
    }
}