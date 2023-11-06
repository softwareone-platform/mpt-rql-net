using SoftwareOne.Rql.Client;
using SoftwareOne.Rql.Linq.Client.Builder.Filter;
using SoftwareOne.Rql.Linq.Client.Builder.Order;
using SoftwareOne.Rql.Linq.Client.Builder.Select;
using SoftwareOne.Rql.Linq.Client.Generator;
using System.Linq.Expressions;

namespace SoftwareOne.Rql.Linq.Client.Builder.Request;

internal class RqlRequestBuilderContext<T> : IRqlRequestBuilderContext<T> where T : class
{
    private readonly IRqlRequestGenerator _generator;
    private IOperator? _filterOperator = null;
    private OrderContext<T>? _orderContext = null;
    private SelectContext<T>? _selectContext = null;

    public RqlRequestBuilderContext(IRqlRequestGenerator generator)
    {
        _generator = generator;
    }

    public RqlRequest Build()
    {
        return _generator.Generate(_filterOperator, _orderContext, _selectContext);
    }

    public IFilteredRqlRequestBuilder<T> Where(Func<IFilterContext<T>, IOperator> filter)
    {
        var filterContext = new FilterContext<T>();
        _filterOperator = filter(filterContext);
        return this;
    }

    public IRqlRequestBuilder Select(Func<ISelectContext<T>, ISelectContext<T>> select)
    {
        _selectContext = new SelectContext<T>();
        select(_selectContext);
        return this;
    }

    public IOrderedRqlRequestBuilder<T> OrderBy<TValue>(Expression<Func<T, TValue>> order)
    {
        AddOrder(order, OrderDirection.Ascending);
        return this;
    }

    public IOrderedRqlRequestBuilder<T> OrderByDescending<TValue>(Expression<Func<T, TValue>> order)
    {
        AddOrder(order, OrderDirection.Descending);
        return this;
    }

    public IOrderedRqlRequestBuilder<T> ThenBy<TValue>(Expression<Func<T, TValue>> orderExpression) => OrderBy(orderExpression);

    public IOrderedRqlRequestBuilder<T> ThenByDescending<TValue>(Expression<Func<T, TValue>> orderExpression) => OrderByDescending(orderExpression);

    private void AddOrder<TValue>(Expression<Func<T, TValue>> orderExpression, OrderDirection direction)
    {
        _orderContext ??= new OrderContext<T>();
        _orderContext.AddOrder(orderExpression, direction);
    }
}