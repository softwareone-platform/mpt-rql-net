namespace SoftwareOne.Rql.Linq.Services.Mapping;

internal interface IMappingService<in TStorage, out TView>
{
    public IQueryable<TView> Apply(IQueryable<TStorage> query);
}
