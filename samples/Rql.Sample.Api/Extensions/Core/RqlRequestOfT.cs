using Microsoft.AspNetCore.Http;
using SoftwareOne.Rql;

namespace Rql.Sample.Api.Extensions.Core;

internal class RqlRequest<TView> : RqlRequest<TView, TView>, IRqlRequest<TView>
{
    public RqlRequest(IHttpContextAccessor httpContextAccessor,
        IErrorResultProvider errorResultProvider,
        IRqlQueryable<TView, TView> rql) : base(httpContextAccessor, errorResultProvider, rql) { }
}
