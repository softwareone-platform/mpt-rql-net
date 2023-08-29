using SoftwareOne.Rql.Linq.Client.Dsl;
using SoftwareOne.Rql.Linq.Client.Order;

#pragma warning disable IDE0130
namespace SoftwareOne.Rql.Client;

public class QueryBuilder<T> where T : class
{
    private Func<QueryContext<T>, IOperator> QueryFunc { get; set; } = context => new EmptyOperator();
    private Func<SelectContext<T>, SelectContext<T>> SelectFunc { get; set; } = context => new SelectContext<T>();
    private Func<OrderContext<T>, OrderContext<T>> OrderFunc { get; set; } = context => new OrderContext<T>();
    private Paging Paging { get; set; } = new DefaultPaging();

    public QueryBuilder()
    {
    }

    public QueryBuilder(Func<QueryContext<T>, IOperator> queryFunc)
    {
        WithQuery(queryFunc);
    }

    public QueryBuilder<T> WithQuery(Func<QueryContext<T>, IOperator> queryFunc)
    {
        QueryFunc = queryFunc;
        return this;
    }

    public QueryBuilder<T> WithPaging(int limit, int offset)
    {
        Paging = new CustomPaging(limit, offset);
        return this;
    }

    public QueryBuilder<T> WithSelect(Func<SelectContext<T>, SelectContext<T>> selectFunc)
    {
        SelectFunc = selectFunc;
        return this;
    }

    public QueryBuilder<T> WithOrder(Func<OrderContext<T>, OrderContext<T>> orderFunc)
    {
        OrderFunc = orderFunc;
        return this;
    }

    public Query Build()
    {
        var select = SelectFunc(new SelectContext<T>()).GetDefinition();
        var order = OrderFunc(new OrderContext<T>()).GetDefinition();
        return new Query(QueryFunc(new QueryContext<T>()), Paging, select, order);
    }
}