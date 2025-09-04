using SoftwareOne.Rql.Abstractions;
using SoftwareOne.Rql.Abstractions.Argument;
using SoftwareOne.Rql.Abstractions.Result;
using SoftwareOne.Rql.Linq.Core;
using SoftwareOne.Rql.Linq.Services.Context;
using System.Linq.Expressions;
using System.Reflection;

namespace SoftwareOne.Rql.Linq.Services.Ordering;

internal sealed class OrderingService<TView> : RqlService, IOrderingService<TView>
{
    private readonly IQueryContext<TView> _context;
    private readonly IOrderingGraphBuilder<TView> _graphBuilder;
    private readonly IOrderingPathInfoBuilder _pathBuilder;
    private readonly IRqlParser _parser;

    public OrderingService(IQueryContext<TView> context, IOrderingGraphBuilder<TView> graphBuilder, IRqlParser parser, IOrderingPathInfoBuilder pathBuilder) : base()
    {
        _context = context;
        _graphBuilder = graphBuilder;
        _parser = parser;
        _pathBuilder = pathBuilder;
    }

    protected override string ErrorPrefix => "order";

    public void Process(string? order)
    {
        if (string.IsNullOrEmpty(order))
            return;

        var node = _parser.Parse(order);

        _graphBuilder.TraverseRqlExpression(_context.Graph, node);

        var orderProperties = node.Items!.OfType<RqlConstant>().ToList();
        if (!orderProperties.Any())
        {
            _context.AddError(Error.Validation("No valid ordering properties were detected", MakeErrorCode("no_props")));
            return;
        }

        var isFirst = true;
        var param = Expression.Parameter(typeof(TView));

        foreach (var property in orderProperties)
        {
            var (path, isAsc) = StringHelper.ExtractSign(property.Value);
            var member = _pathBuilder.Build(param, path.ToString());

            if (member.IsError)
            {
                _context.AddErrors(member.Errors);
                continue;
            }

            var method = MakeOrderingMethod(member.Value!.Expression, isAsc, isFirst);
            var expression = Expression.Lambda(member.Value.Expression, param);

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