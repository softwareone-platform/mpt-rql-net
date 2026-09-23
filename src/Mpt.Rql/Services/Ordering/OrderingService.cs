using Mpt.Rql.Abstractions;
using Mpt.Rql.Abstractions.Argument;
using Mpt.Rql.Abstractions.Argument.Pointer;
using Mpt.Rql.Abstractions.Group;
using Mpt.Rql.Abstractions.Result;
using Mpt.Rql.Core;
using Mpt.Rql.Services.Context;
using Mpt.Rql.Services.Ordering.Functions;
using System.Linq.Expressions;
using System.Reflection;

namespace Mpt.Rql.Services.Ordering;

internal sealed class OrderingService<TView> : RqlService, IOrderingService<TView>
{
    private readonly IQueryContext<TView> _context;
    private readonly IOrderingGraphBuilder<TView> _graphBuilder;
    private readonly IRqlParser _parser;
    private readonly OrderingFunctionServices _services;

    public OrderingService(
        IQueryContext<TView> context,
        IOrderingGraphBuilder<TView> graphBuilder,
        IRqlParser parser,
        OrderingFunctionServices services) : base()
    {
        _context = context;
        _graphBuilder = graphBuilder;
        _parser = parser;
        _services = services;
    }

    protected override string ErrorPrefix => "order";

    public void Process(string? order)
    {
        if (string.IsNullOrEmpty(order))
            return;

        if (!TryParse(_parser, order, out var node, out var parseError))
        {
            _context.AddError(parseError!);
            return;
        }

        _graphBuilder.TraverseRqlExpression(_context.Graph, node);

        // A single function call at the root parses to a named RqlGenericGroup (or a member access over one:
        // first(...).value). Anything else parses to a group whose items are the individual order terms.
        // Anonymous and sign-only groups are plain lists of terms, not function calls.
        List<RqlExpression> orderItems = IsFunctionCall(node)
            ? [node]
            : node.Items!.Where(item => item is RqlConstant || IsFunctionCall(item)).ToList();

        if (orderItems.Count == 0)
        {
            _context.AddError(Error.Validation("No valid ordering properties were detected", MakeErrorCode("no_props")));
            return;
        }

        var isFirst = true;
        var param = Expression.Parameter(typeof(TView));

        foreach (var item in orderItems)
        {
            var resolved = item switch
            {
                RqlConstant constant => ResolveConstantOrder(constant, param),
                RqlGenericGroup group => ResolveFunctionOrder(group, memberPath: null, param),
                RqlMemberAccess { Inner: RqlGenericGroup group } member => ResolveFunctionOrder(group, member.Path, param),
                _ => null
            };

            if (resolved is null)
                continue;

            var (keyExpression, isAsc) = resolved.Value;

            var method = MakeOrderingMethod(keyExpression, isAsc, isFirst);
            var expression = Expression.Lambda(keyExpression, param);

            _context.AddTransformation(q => (IQueryable<TView>)method.Invoke(null, [q, expression])!);
            isFirst = false;
        }
    }

    private static bool IsFunctionCall(RqlExpression expression)
        => expression switch
        {
            RqlGenericGroup { Name: { Length: > 0 } name } => StringHelper.ExtractSign(name).value.Length > 0,
            RqlMemberAccess { Inner: RqlGenericGroup inner } => IsFunctionCall(inner),
            _ => false,
        };

    private (Expression KeyExpression, bool IsAsc)? ResolveConstantOrder(RqlConstant constant, ParameterExpression param)
    {
        var (path, isAsc) = StringHelper.ExtractSign(constant.Value);

        var member = _services.PathBuilder.Build(param, path.ToString());
        if (member.IsError)
        {
            _context.AddErrors(member.Errors);
            return null;
        }

        return (member.Value!.Expression, isAsc);
    }

    private (Expression KeyExpression, bool IsAsc)? ResolveFunctionOrder(RqlGenericGroup group, string? memberPath, ParameterExpression param)
    {
        var (nameMemory, isAsc) = StringHelper.ExtractSign(group.Name);
        var name = nameMemory.ToString();

        if (!_services.Functions.TryGet(name, out var function))
        {
            _context.AddError(Error.Validation($"Unknown ordering function '{name}'.", OrderingErrorCodes.UnknownFunction));
            return null;
        }

        var context = new OrderingFunctionContext(
            param,
            group.Items ?? [],
            memberPath,
            _services.PathBuilder,
            _services.FilterBuilder,
            _services.BuilderContext,
            _services.Settings);

        var key = function.Build(context);
        if (key.IsError)
        {
            _context.AddErrors(key.Errors);
            return null;
        }

        return (key.Value!, isAsc);
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
