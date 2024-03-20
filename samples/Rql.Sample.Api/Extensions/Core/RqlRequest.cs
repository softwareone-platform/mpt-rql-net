using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Rql.Sample.Contracts.Ef.Products;
using Rql.Sample.Domain.Ef;
using SoftwareOne.Rql;
using System.Globalization;
using System.Linq.Expressions;

namespace Rql.Sample.Api.Extensions.Core;

internal class RqlRequest<TStorage, TView> : IRqlRequest<TStorage, TView>
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IErrorResultProvider _errorResultProvider;
    private readonly IRqlQueryable<TStorage, TView> _rql;

    public RqlRequest(IHttpContextAccessor httpContextAccessor, IErrorResultProvider errorResultProvider, IRqlQueryable<TStorage, TView> rql)
    {
        _httpContextAccessor = httpContextAccessor;
        _errorResultProvider = errorResultProvider;
        _rql = rql;
    }

    public async Task<IActionResult> ProcessAsync(IQueryable<TStorage> source, Expression<Func<TStorage, TView>>? map = null)
    {
        var httpContext = _httpContextAccessor.HttpContext!;
        var request = ExtractRqlRequest(httpContext.Request.Query);
        var res = _rql.Transform(source, request);

        if (res.Status.IsError)
            return _errorResultProvider.Problem(res.Status.Errors);

        var limit = ParseIntParameter(httpContext.Request.Query, QueryConstants.Limit, 10);
        var offset = ParseIntParameter(httpContext.Request.Query, QueryConstants.Offset, 0);

        var data = new ListResponse<TView>
        {
            Metadata = new ListResponseMetadata
            {
                Pagination = new PaginationMetadata
                {
                    Limit = limit,
                    Offset = offset
                }
            }
        };

        if (res.Query is IAsyncEnumerable<TView>) // efcore
        {
            data.Data = await res.Query.Skip(offset).Take(limit).ToListAsync();
            data.Metadata.Pagination.Total = await res.Query.CountAsync();
        }
        else // memory
        {
            data.Data = res.Query.Skip(offset).Take(limit).ToList();
            data.Metadata.Pagination.Total = res.Query.Count();
        }

        data.Metadata.Omitted = res.Graph.Children.Where(t => !t.IsIncluded && (t.ExcludeReason & (ExcludeReasons.Unselected | ExcludeReasons.Default)) != 0).Select(s => s.Name).ToList();

        return new OkObjectResult(data);

        static int ParseIntParameter(IQueryCollection query, string name, int defaultValue)
        {
            var value = query[name];
            if (!int.TryParse(value, out var result))
                result = defaultValue;
            return result;
        }
    }

    private static RqlRequest ExtractRqlRequest(IQueryCollection query)
    {
        var request = new RqlRequest();
        var filterItems = new List<string>();

        foreach (var item in query)
        {
            if (item.Key.Equals(QueryConstants.Limit, StringComparison.InvariantCultureIgnoreCase) ||
                item.Key.Equals(QueryConstants.Offset, StringComparison.InvariantCultureIgnoreCase))
                continue;

            var value = item.Value.FirstOrDefault()!;

            switch (item.Key.ToLower(CultureInfo.InvariantCulture))
            {
                case QueryConstants.Query:
                    filterItems.Add(value);
                    break;
                case QueryConstants.Select:
                    request.Select = value;
                    break;
                case QueryConstants.Order:
                    request.Order = value;
                    break;
                default:
                    if (!string.IsNullOrEmpty(value))
                        filterItems.Add($"{item.Key}={value}");
                    else
                        filterItems.Add(item.Key);
                    break;
            }
        }

        if (filterItems.Count > 0)
            request.Filter = string.Join(",", filterItems);

        return request;
    }
}
