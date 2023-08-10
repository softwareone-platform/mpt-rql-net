using Microsoft.Extensions.DependencyInjection;

namespace SoftwareOne.Rql.Linq.Services.Mapping;

internal class MappingService<TStorage, TView> : IMappingService<TStorage, TView>
{
    private readonly IServiceProvider _serviceProvider;

    public MappingService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IQueryable<TView> Apply(IQueryable<TStorage> query)
    {
        if (typeof(TStorage) == typeof(TView))
        {
            return (IQueryable<TView>)query;
        }
        
        var map = _serviceProvider.GetService<IRqlMapper<TStorage, TView>>() 
            ?? throw new Exception($"Mapping between '{typeof(TStorage)}' and '{typeof(TView)}' is required but not defined.");
        
        return query.Select(map.GetMapping());
    }
}