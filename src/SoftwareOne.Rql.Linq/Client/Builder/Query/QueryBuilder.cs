namespace SoftwareOne.Rql.Client;

public class QueryBuilder<T> : QueryBuilderBase<T> where T : class
{
    public QueryBuilder()
    {
    }

    public QueryBuilderWithQuery<T> WithQuery(Func<QueryContext<T>, IOperator> queryFunc)
    {
        QueryFunc = queryFunc;
        return new QueryBuilderWithQuery<T>(this);
    }

    public static QueryBuilderWithQuery<T> FromQuery(Func<QueryContext<T>, IOperator> queryFunc)
    {
        return new QueryBuilder<T>().WithQuery(queryFunc);
    }
}