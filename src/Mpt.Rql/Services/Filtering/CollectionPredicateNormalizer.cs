using System.Linq.Expressions;
using static Mpt.Rql.Core.CollectionQueryHelper;

namespace Mpt.Rql.Services.Filtering;

/// <summary>
/// Normalizes collection predicates without rewriting unrelated view predicates.
/// </summary>
internal static class CollectionPredicateNormalizer
{
    /// <summary>
    /// Normalizes collection predicates using the source of a mapped query.
    /// </summary>
    public static IQueryable<TView> Apply<TView>(
        IQueryable<TView> query,
        Expression<Func<TView, bool>> filter)
    {
        if (query.Expression is not MethodCallExpression select ||
            select.Method.DeclaringType != typeof(Queryable) ||
            select.Method.Name != nameof(Queryable.Select) ||
            select.Arguments.Count != 2 ||
            select.Arguments[1] is not UnaryExpression
            {
                NodeType: ExpressionType.Quote,
                Operand: LambdaExpression { Parameters.Count: 1 } projection
            })
        {
            return query.Where(filter);
        }

        var sourceParameter = projection.Parameters[0];
        var rowType = typeof(CollectionPredicateRow<,>)
            .MakeGenericType(sourceParameter.Type, typeof(TView));

        var rowParameter = Expression.Parameter(rowType, "row");
        var sourceProperty = rowType.GetProperty(
            nameof(CollectionPredicateRow<object, object>.Source))!;
        var viewProperty = rowType.GetProperty(
            nameof(CollectionPredicateRow<object, object>.View))!;

        var rowSource = Expression.Property(rowParameter, sourceProperty);
        var rowView = Expression.Property(rowParameter, viewProperty);

        var visitor = new PredicateVisitor(
            filter.Parameters[0],
            projection,
            rowSource,
            rowView);

        var predicateBody = visitor.Visit(filter.Body)!;

        if (!visitor.HasNormalizedPredicates)
            return query.Where(filter);

        var rowInitializer = Expression.MemberInit(
            Expression.New(rowType),
            Expression.Bind(sourceProperty, sourceParameter),
            Expression.Bind(viewProperty, projection.Body));

        var rowProjection = Expression.Lambda(
            rowInitializer,
            sourceParameter);

        var projectedRows = Expression.Call(
            QueryableSelect.MakeGenericMethod(sourceParameter.Type, rowType),
            select.Arguments[0],
            Expression.Quote(rowProjection));

        var rowPredicate = Expression.Lambda(
            predicateBody,
            rowParameter);

        var filteredRows = Expression.Call(
            QueryableWhere.MakeGenericMethod(rowType),
            projectedRows,
            Expression.Quote(rowPredicate));

        var resultProjection = Expression.Lambda(
            rowView,
            rowParameter);

        var result = Expression.Call(
            QueryableSelect.MakeGenericMethod(rowType, typeof(TView)),
            filteredRows,
            Expression.Quote(resultProjection));

        return query.Provider.CreateQuery<TView>(result);
    }

    private sealed class PredicateVisitor(
        ParameterExpression viewParameter,
        LambdaExpression projection,
        Expression rowSource,
        Expression rowView) : ExpressionVisitor
    {
        public bool HasNormalizedPredicates { get; private set; }

        protected override Expression VisitParameter(ParameterExpression node)
            => node == viewParameter ? rowView : node;

        protected override Expression VisitBinary(BinaryExpression node)
        {
            if (IsGuardedQuantifier(node))
                return Normalize(node);

            return base.VisitBinary(node);
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (IsQuantifier(node))
                return Normalize(node);

            return base.VisitMethodCall(node);
        }

        protected override Expression VisitLambda<T>(Expression<T> node)
            => node;

        private Expression Normalize(Expression predicate)
        {
            var viewPredicate = Expression.Lambda(
                predicate,
                viewParameter);

            var sourcePredicate = FilterPushdownVisitor.ToSourceFilter(
                viewPredicate,
                projection,
                removeRedundantNullChecks: false);

            HasNormalizedPredicates = true;

            return new ParameterReplacementVisitor(
                    projection.Parameters[0],
                    rowSource)
                .Visit(sourcePredicate.Body)!;
        }
    }

    private sealed class ParameterReplacementVisitor(
        ParameterExpression parameter,
        Expression replacement) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node)
            => node == parameter ? replacement : node;
    }
}

/// <summary>
/// Carries the source and its mapped view through collection filtering.
/// </summary>
internal sealed class CollectionPredicateRow<TSource, TView>
{
    public TSource Source { get; set; } = default!;

    public TView View { get; set; } = default!;
}