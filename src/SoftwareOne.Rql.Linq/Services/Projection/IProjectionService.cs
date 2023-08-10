using ErrorOr;

namespace SoftwareOne.Rql.Linq.Services.Projection;

internal interface IProjectionService<TView>
{
    public ErrorOr<IQueryable<TView>> Apply(IQueryable<TView> query, string? projection);
}