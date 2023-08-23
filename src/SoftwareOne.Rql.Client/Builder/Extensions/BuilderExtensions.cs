using SoftwareOne.Rql.Client.Builder.Query;
using SoftwareOne.Rql.Client.RqlGenerator;

namespace SoftwareOne.Rql.Client.Builder.Extensions;

public static class BuilderExtensions
{
    public static RqlGenerator.Rql ToRql(this Client.Query query)
    {
        return new QueryGenerator(new QueryParamsGenerator(), new SelectGenerator(), new PagingGenerator(), new OrderGenerator()).Generate(query);
    }


    public static string BuildString<T>(this QueryBuilder<T> queryBuilder) where T : class
    {
        return queryBuilder.Build().ToRql().ToString();
    }
}