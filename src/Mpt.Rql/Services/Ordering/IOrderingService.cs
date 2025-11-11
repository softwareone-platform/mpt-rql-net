namespace Mpt.Rql.Services.Ordering;

internal interface IOrderingService<TView>
{
    public void Process(string? order);
}