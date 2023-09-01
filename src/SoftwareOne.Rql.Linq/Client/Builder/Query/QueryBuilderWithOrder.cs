namespace SoftwareOne.Rql.Client;

public class QueryBuilderWithOrder<T> : QueryBuilderBase<T> where T : class
{
    public QueryBuilderWithOrder(QueryBuilderBase<T> previous) : base(previous)
    {
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