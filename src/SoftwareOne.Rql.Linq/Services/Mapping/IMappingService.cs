using ErrorOr;

namespace SoftwareOne.Rql.Linq.Services.Mapping;

internal interface IMappingService<in TStorage, TView>
{
    IQueryable<TView> Apply(IQueryable<TStorage> query);
}
