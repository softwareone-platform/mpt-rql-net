namespace Mpt.Rql.Services.Mapping;

internal interface IMappingService<in TStorage, TView>
{
    IQueryable<TView> Apply(IQueryable<TStorage> query);
}
