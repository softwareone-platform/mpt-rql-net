using Microsoft.AspNetCore.Mvc;

#pragma warning disable IDE0130
namespace SoftwareOne.Rql
{
    public interface IRqlRequest<TStorage, TView>
    {
        public Task<IActionResult> ProcessAsync(IQueryable<TStorage> source);
    }

    public interface IRqlRequest<TView> : IRqlRequest<TView, TView> { }
}
