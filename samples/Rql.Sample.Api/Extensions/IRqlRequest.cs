using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;

#pragma warning disable IDE0130
namespace Mpt.Rql;

public interface IRqlRequest<TStorage, TView>
{
    public Task<IActionResult> ProcessAsync(IQueryable<TStorage> source, Expression<Func<TStorage, TView>>? map = null);
}

public interface IRqlRequest<TView> : IRqlRequest<TView, TView> { }
