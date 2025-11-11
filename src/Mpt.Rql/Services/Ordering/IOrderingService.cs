namespace Mpt.Rql.Linq.Services.Ordering;

internal interface IOrderingService<TView>
{
    public void Process(string? order);
}