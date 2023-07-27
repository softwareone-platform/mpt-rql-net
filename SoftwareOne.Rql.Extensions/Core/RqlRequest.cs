using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SoftwareOne.Rql;
using SoftwareOne.Rql.Linq;

namespace SoftwareOne.Rql.Extensions.Core
{
    internal class RqlRequest<TView> : RqlRequest<TView, TView>, IRqlRequest<TView>
    {
        public RqlRequest(IHttpContextAccessor httpContextAccessor,
            IErrorResultProvider errorResultProvider,
            IRqlQueryable<TView, TView> rql) : base(httpContextAccessor, errorResultProvider, rql) { }
    }

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

        public async Task<IActionResult> ProcessAsync(IQueryable<TStorage> source)
        {
            var httpContext = _httpContextAccessor.HttpContext!;
            var request = ExtractRqlRequest(httpContext.Request.Query);
            var res = _rql.Transform(source, request);

            if (res.IsError)
                return _errorResultProvider.Problem(res.Errors);

            var limit = ParseIntParameter(httpContext.Request.Query, QueryConstants.Limit, 10);
            var offset = ParseIntParameter(httpContext.Request.Query, QueryConstants.Offset, 0);

            List<TView> data;
            int count;
            if (res.Value is IAsyncEnumerable<TView>) // efcore
            {
                data = await res.Value.Skip(offset).Take(limit).ToListAsync();
                count = await res.Value.CountAsync();
            }
            else // memory
            {
                data = res.Value.Skip(offset).Take(limit).ToList();
                count = res.Value.Count();
            }

            httpContext.Response.Headers.Add(QueryConstants.TotalCountHeaderName, count.ToString());

            return new OkObjectResult(data);

            static int ParseIntParameter(IQueryCollection query, string name, int defaultValue)
            {
                var value = query[name];
                if (!int.TryParse(value, out var result))
                    result = defaultValue;
                return result;
            }
        }

        private RqlRequest ExtractRqlRequest(IQueryCollection query)
        {
            var request = new RqlRequest();
            var filterItems = new List<string>();

            foreach (var item in query)
            {
                if (item.Key.Equals(QueryConstants.Limit, StringComparison.InvariantCultureIgnoreCase) ||
                    item.Key.Equals(QueryConstants.Offset, StringComparison.InvariantCultureIgnoreCase))
                    continue;

                var value = item.Value.FirstOrDefault()!;

                switch (item.Key)
                {
                    case var s when s.Equals(QueryConstants.Query, StringComparison.InvariantCultureIgnoreCase):
                        filterItems.Add(value);
                        break;
                    case var s when s.Equals(QueryConstants.Select, StringComparison.InvariantCultureIgnoreCase):
                        request.Select = value;
                        break;
                    case var s when s.Equals(QueryConstants.Order, StringComparison.InvariantCultureIgnoreCase):
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
}
