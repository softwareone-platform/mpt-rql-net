using Mpt.Rql.Abstractions;
using Mpt.Rql.Abstractions.Argument;
using Mpt.Rql.Abstractions.Group;
using Mpt.Rql.Abstractions.Result;
using Mpt.Rql.Core;
using Mpt.Rql.Services.Context;
using System.Linq.Expressions;
using System.Reflection;

namespace Mpt.Rql.Services.Ordering;

internal sealed class OrderingService<TView> : RqlService, IOrderingService<TView>
{
    private readonly IQueryContext<TView> _context;
    private readonly IOrderingGraphBuilder<TView> _graphBuilder;
    private readonly IOrderingPathInfoBuilder _pathBuilder;
    private readonly IRqlParser _parser;
    private readonly IOrderingFunctionProvider _functionProvider;

    public OrderingService(
        IQueryContext<TView> context,
        IOrderingGraphBuilder<TView> graphBuilder,
        IRqlParser parser,
        IOrderingPathInfoBuilder pathBuilder,
        IOrderingFunctionProvider functionProvider) : base()
    {
        _context = context;
        _graphBuilder = graphBuilder;
        _parser = parser;
        _pathBuilder = pathBuilder;
        _functionProvider = functionProvider;
    }

    protected override string ErrorPrefix => "order";

    public void Process(string? order)
    {
        if (string.IsNullOrEmpty(order))
            return;

        var node = _parser.Parse(order);

        _graphBuilder.TraverseRqlExpression(_context.Graph, node);

        // When the entire ordering string is a single function call (e.g. +orderby(...)),
        // the parser returns the RqlGenericGroup directly as the root node — its Name holds
        // the function name (with sign prefix) and its Items hold the function arguments.
        // For multi-item expressions (e.g. "+name,-id") the root is an RqlAnd whose Items
        // are the individual sort terms. The guard below normalises both shapes.
        var orderItems = node is RqlGenericGroup { Name.Length: > 0 }
            ? [(RqlExpression)node]
            : node.Items!
                .Where(item => item is RqlConstant || item is RqlGenericGroup)
                .ToList();

        if (orderItems.Count == 0)
        {
            _context.AddError(Error.Validation("No valid ordering properties were detected", MakeErrorCode("no_props")));
            return;
        }

        var isFirst = true;
        var param = Expression.Parameter(typeof(TView));

        foreach (var item in orderItems)
        {
            var resolved = ResolveOrderItem(item, param);
            if (resolved == null)
                continue;

            var (keyExpression, isAsc) = resolved.Value;

            var method = MakeOrderingMethod(keyExpression, isAsc, isFirst);
            var expression = Expression.Lambda(keyExpression, param);

            _context.AddTransformation(q => (IQueryable<TView>)method.Invoke(null, new object[] { q, expression })!);
            isFirst = false;
        }
    }

    private (Expression KeyExpression, bool IsAsc)? ResolveOrderItem(RqlExpression item, ParameterExpression param)
    {
        if (item is RqlConstant constant)
            return ResolveConstantOrder(constant, param);

        if (item is RqlGenericGroup group)
            return ResolveFunctionOrder(group, param);

        return null;
    }

    private (Expression KeyExpression, bool IsAsc)? ResolveConstantOrder(RqlConstant constant, ParameterExpression param)
    {
        var (path, isAsc) = StringHelper.ExtractSign(constant.Value);

        var member = _pathBuilder.Build(param, path.ToString());
        if (member.IsError)
        {
            _context.AddErrors(member.Errors);
            return null;
        }

        return (member.Value!.Expression, isAsc);
    }

    private (Expression KeyExpression, bool IsAsc)? ResolveFunctionOrder(RqlGenericGroup group, ParameterExpression param)
    {
        var (funcNameMemory, isAsc) = StringHelper.ExtractSign(group.Name);
        var funcName = funcNameMemory.ToString();

        if (!_functionProvider.TryGet(funcName, out var function))
        {
            _context.AddError(Error.Validation(
                $"Unknown ordering function '{funcName}'.",
                MakeErrorCode("unknown_func")));
            return null;
        }

        var arguments = (group.Items ?? [])
            .OfType<RqlConstant>()
            .Select(c => c.Value)
            .ToArray();

        var funcResult = function.Build(param, arguments);
        if (funcResult.IsError)
        {
            _context.AddErrors(funcResult.Errors);
            return null;
        }

        return (funcResult.Value!, isAsc);
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
