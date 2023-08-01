namespace SoftwareOne.Rql.Linq.Services.Mapping
{
    internal interface IMappingService<TStorage, TView>
    {
        public IQueryable<TView> Apply(IQueryable<TStorage> query);
    }
}
