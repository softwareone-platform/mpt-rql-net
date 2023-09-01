using SoftwareOne.Rql.Linq.Client;

#pragma warning disable IDE0130
namespace SoftwareOne.Rql.Client;

public static class BuilderExtensions
{
    private static Rql ToRql(this Query query)
    {
        return new QueryGenerator(new QueryParamsGenerator(), new SelectGenerator(), new PagingGenerator(),
            new OrderGenerator()).Generate(query);
    }


    public static string BuildString<T>(this QueryBuilderBase<T> queryBuilder) where T : class
    {
        return ToRql(((IQueryBuilder)queryBuilder).Build()).ToString();
    }
}