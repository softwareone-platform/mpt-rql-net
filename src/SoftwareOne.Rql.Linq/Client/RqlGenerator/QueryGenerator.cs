using SoftwareOne.Rql.Client;

namespace SoftwareOne.Rql.Linq.Client;

internal class QueryGenerator : IQueryGenerator
{
    private readonly IQueryParamsGenerator _queryParamsGenerator;
    private readonly ISelectGenerator _selectGenerator;
    private readonly IPagingGenerator _pagingGenerator;
    private readonly IOrderGenerator _orderGenerator;

    public QueryGenerator(IQueryParamsGenerator queryParamsGenerator, ISelectGenerator selectGenerator, IPagingGenerator pagingGenerator, IOrderGenerator orderGenerator)
    {
        _queryParamsGenerator = queryParamsGenerator;
        _selectGenerator = selectGenerator;
        _pagingGenerator = pagingGenerator;
        _orderGenerator = orderGenerator;
    }

    public Rql.Client.Rql Generate(Query query)
    {
        var queryParams = _queryParamsGenerator.Generate(query.Op);
        var selectParams = _selectGenerator.Generate(query.Select);
        var pagingParams = _pagingGenerator.Generate(query.Paging);
        var orderParams = _orderGenerator.Generate(query.Order);
        return new Rql.Client.Rql(queryParams, selectParams, pagingParams, orderParams);
    }
}