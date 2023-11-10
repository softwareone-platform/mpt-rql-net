using ErrorOr;
using Microsoft.Extensions.DependencyInjection;

namespace SoftwareOne.Rql.Linq.Services.Mapping;

internal class MappingService<TStorage, TView> : IMappingService<TStorage, TView>
{
    private readonly IServiceProvider _serviceProvider;

    public MappingService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public ErrorOr<IQueryable<TView>> Apply(IQueryable<TStorage> query)
    {
        if (typeof(TStorage) == typeof(TView))
        {
            return ErrorOrFactory.From((IQueryable<TView>)query);
        }

        var mapService = _serviceProvider.GetService<IRqlMapper<TStorage, TView>>();

        if (mapService == null)
            return Error.Failure(description: $"Mapping between '{typeof(TStorage)}' and '{typeof(TView)}' is required but not defined.");

        return ErrorOrFactory.From(query.Select(mapService.GetMapping()));
    }
}