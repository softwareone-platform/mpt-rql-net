namespace SoftwareOne.Rql.Client;

public class QueryBuilderWithPaging<T> : QueryBuilderBase<T> where T : class
{
    public QueryBuilderWithPaging(QueryBuilderBase<T> previous) : base(previous)
    {
    }

    public Query Build()
    {
        return ((IQueryBuilder)this).Build();
    }
}