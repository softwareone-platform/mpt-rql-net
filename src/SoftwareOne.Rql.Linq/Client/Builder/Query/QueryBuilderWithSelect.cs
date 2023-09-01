using SoftwareOne.Rql.Linq.Client.Order;

namespace SoftwareOne.Rql.Client;

public class QueryBuilderWithSelect<T> : QueryBuilderBase<T> where T : class
{
    public QueryBuilderWithSelect(QueryBuilderBase<T> previous) : base(previous)
    {
    }

    public QueryBuilderWithOrder<T> WithOrder(Func<OrderContext<T>, OrderContext<T>> orderFunc)
    {
        OrderFunc = orderFunc;
        return new QueryBuilderWithOrder<T>(this);
    }

    public QueryBuilderWithPaging<T> WithPaging(int limit, int offset)
    {
        Paging = new CustomPaging(limit, offset);
        return new QueryBuilderWithPaging<T>(this);
    }

    public Query Build()
    {
        return ((IQueryBuilder)this).Build();
    }
}