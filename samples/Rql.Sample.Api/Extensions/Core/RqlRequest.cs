using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        var res = _rql.Transform(source, request, out var auditContext);

        if (res.IsError)
            return _errorResultProvider.Problem(res.Errors);

        var limit = ParseIntParameter(httpContext.Request.Query, QueryConstants.Limit, 10);
        var offset = ParseIntParameter(httpContext.Request.Query, QueryConstants.Offset, 0);

        var data = new ListResponse<TView>
        {
            Pagination = new PaginationMetadata
            {
                Limit = limit,
                Offset = offset
            }
        };

        if (res.Value is IAsyncEnumerable<TView>) // efcore
        {
            data.Data = await res.Value.Skip(offset).Take(limit).ToListAsync();
            data.Pagination.Total = await res.Value.CountAsync();
        }
        else // memory
        {
            data.Data = res.Value.Skip(offset).Take(limit).ToList();
            data.Pagination.Total = res.Value.Count();
        }
        
        data.Omitted = auditContext.Omitted.Where(s=> !s.Contains('.')).ToList();

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
