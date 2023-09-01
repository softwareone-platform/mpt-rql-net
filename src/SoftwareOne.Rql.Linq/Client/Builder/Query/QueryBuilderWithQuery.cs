namespace SoftwareOne.Rql.Client;

public class QueryBuilderWithQuery<T> : QueryBuilderBase<T> where T : class
{
    public QueryBuilderWithQuery(QueryBuilderBase<T> previous) : base(previous)
    {
    }

    public QueryBuilderWithSelect<T> WithSelect(Func<SelectContext<T>, SelectContext<T>> selectFunc)
    {
        SelectFunc = selectFunc;
        return new QueryBuilderWithSelect<T>(this);
    }

    public Query Build()
    {
        return ((IQueryBuilder)this).Build();
    }
}