using ErrorOr;

namespace SoftwareOne.Rql.Linq.Services.Ordering
{
    internal interface IOrderingService<TView>
    {
        public ErrorOr<IQueryable<TView>> Apply(IQueryable<TView> query, string? order);
    }
}
