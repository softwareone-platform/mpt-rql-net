using SoftwareOne.Rql.Client;
using SoftwareOne.Rql.Linq.Client.Filter;
using SoftwareOne.Rql.Linq.Client.Order;
using SoftwareOne.Rql.Linq.Client.Select;

namespace SoftwareOne.Rql.Linq.Client.RqlRequest;

internal class RqlRequestBuilder<T> : IRqlRequestBuilder<T>, IRqlRequestBuilderWithRqlRequest<T>, IRqlRequestBuilderWithOrder<T>, IRqlRequestBuilderWithSelect<T> where T : class
{
    private readonly IRqlRequestGenerator _generator;
    private Func<IFilterContext<T>, IFilterContext<T>>? _filterCallback = null;
    private Func<ISelectContext<T>, ISelectContext<T>>? _selectCallback = null;
    private Func<IOrderBeginContext<T>, IOrderContext>? _orderCallback = null;

    public RqlRequestBuilder(IRqlRequestGenerator generator)
    {
        _generator = generator;
    }

    public Rql.RqlRequest Build()
    {
        SelectContext<T>? select = null;
        if (_selectCallback != null)
        {
            select = new SelectContext<T>();
            _ = _selectCallback(select);
        }

        OrderContext<T>? order = null;
        if (_orderCallback != null)
        {
            order = new OrderContext<T>();
            _ = _orderCallback(order);
        }

        FilterContext<T>? filter = null;
        if (_filterCallback != null)
        {
            filter = new FilterContext<T>();
            _ = _filterCallback(filter);
        }

        return _generator.Generate(filter, order, select);
    }

    public IRqlRequestBuilderWithRqlRequest<T> Where(Func<IFilterContext<T>, IFilterContext<T>> filter)
    {
        _filterCallback = filter;
        return this;
    }

    public IRqlRequestBuilderWithOrder<T> Order(Func<IOrderBeginContext<T>, IOrderContext> order)
    {
        _orderCallback = order;
        return this;
    }

    public IRqlRequestBuilderWithSelect<T> Select(Func<ISelectContext<T>, ISelectContext<T>> select)
    {
        _selectCallback = select;
        return this;
    }
}