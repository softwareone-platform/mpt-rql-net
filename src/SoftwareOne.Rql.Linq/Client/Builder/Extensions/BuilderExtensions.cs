using SoftwareOne.Rql.Linq.Client;
using SoftwareOne.Rql.Linq.Client.RqlGenerator;

#pragma warning disable IDE0130
namespace SoftwareOne.Rql.Client;

public static class BuilderExtensions
{
    public static Rql ToRql(this Query query)
    {
        return new QueryGenerator(new QueryParamsGenerator(), new SelectGenerator(), new PagingGenerator(), new OrderGenerator()).Generate(query);
    }


    public static string BuildString<T>(this QueryBuilder<T> queryBuilder) where T : class
    {
        return queryBuilder.Build().ToRql().ToString();
    }
}