using ErrorOr;

namespace SoftwareOne.Rql.Linq.Services.Filtering;

internal interface IFilteringService<TView>
{
    public ErrorOr<IQueryable<TView>> Apply(IQueryable<TView> query, string? filter);
}