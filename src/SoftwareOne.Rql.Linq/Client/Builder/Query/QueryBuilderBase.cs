using SoftwareOne.Rql.Linq.Client.Dsl;
using SoftwareOne.Rql.Linq.Client.Order;

#pragma warning disable IDE0130
namespace SoftwareOne.Rql.Client;

public class QueryBuilderBase<T> : IQueryBuilder where T : class
{
    protected Func<QueryContext<T>, IOperator> QueryFunc = context => new EmptyOperator();
    protected Func<SelectContext<T>, SelectContext<T>> SelectFunc = context => new SelectContext<T>();
    protected Func<OrderContext<T>, OrderContext<T>> OrderFunc = context => new OrderContext<T>();
    protected Paging Paging = new DefaultPaging();

    public QueryBuilderBase()
    {
    }

    protected QueryBuilderBase(QueryBuilderBase<T> previous)
    {
        QueryFunc = previous.QueryFunc;
        SelectFunc = previous.SelectFunc;
        OrderFunc = previous.OrderFunc;
        Paging = previous.Paging;
    }

    Query IQueryBuilder.Build()
    {
        var select = SelectFunc(new SelectContext<T>()).GetDefinition();
        var order = OrderFunc(new OrderContext<T>()).GetDefinition();
        return new Query(QueryFunc(new QueryContext<T>()), Paging, select, order);
    }
}