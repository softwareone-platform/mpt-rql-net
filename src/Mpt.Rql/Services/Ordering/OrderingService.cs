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

        if (!orderItems.Any())
        {
            _context.AddError(Error.Validation("No valid ordering properties were detected", MakeErrorCode("no_props")));
            return;
        }

        var isFirst = true;
        var param = Expression.Parameter(typeof(TView));

        foreach (var item in orderItems)
        {
            Expression? keyExpression = null;
            bool isAsc;

            if (item is RqlConstant constant)
            {
                var (path, constIsAsc) = StringHelper.ExtractSign(constant.Value);
                isAsc = constIsAsc;

                var member = _pathBuilder.Build(param, path.ToString());
                if (member.IsError)
                {
                    _context.AddErrors(member.Errors);
                    continue;
                }

                keyExpression = member.Value!.Expression;
            }
            else if (item is RqlGenericGroup group)
            {
                var (funcNameMemory, funcIsAsc) = StringHelper.ExtractSign(group.Name);
                isAsc = funcIsAsc;
                var funcName = funcNameMemory.ToString();

                if (!_functionProvider.TryGet(funcName, out var function))
                {
                    _context.AddError(Error.Validation(
                        $"Unknown ordering function '{funcName}'.",
                        MakeErrorCode("unknown_func")));
                    continue;
                }

                var arguments = (group.Items ?? [])
                    .OfType<RqlConstant>()
                    .Select(c => c.Value)
                    .ToArray();

                var funcResult = function.Build(param, arguments);
                if (funcResult.IsError)
                {
                    _context.AddErrors(funcResult.Errors);
                    continue;
                }

                keyExpression = funcResult.Value!;
            }
            else
            {
                continue;
            }

            var method = MakeOrderingMethod(keyExpression, isAsc, isFirst);
            var expression = Expression.Lambda(keyExpression, param);

            _context.AddTransformation(q => (IQueryable<TView>)method.Invoke(null, new object[] { q, expression })!);
            isFirst = false;
        }
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
