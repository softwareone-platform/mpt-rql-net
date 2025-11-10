using System.Collections.Concurrent;

namespace Mpt.Rql.Linq.Services.Mapping;

internal interface IEntityMapCache
{
    Dictionary<string, RqlMapEntry> Get(Type typeFrom, Type typeTo);
}

internal class EntityMapCache(IServiceProvider serviceProvider) : IEntityMapCache
{
    private readonly ConcurrentDictionary<(Type, Type), Dictionary<string, RqlMapEntry>> _cache = [];

    private readonly IServiceProvider _serviceProvider = serviceProvider;

    public Dictionary<string, RqlMapEntry> Get(Type typeFrom, Type typeTo)
    {
        var key = (typeFrom, typeTo);
        return _cache.GetOrAdd(key, k =>
        {
            IRqlMapper? mapper = null;

            var currentTypeFrom = k.Item1;
            var typeTo = k.Item2;

            while (currentTypeFrom != null)
            {
                var mapperInterface = typeof(IRqlMapper<,>).MakeGenericType(currentTypeFrom, typeTo);
                mapper = _serviceProvider.GetService(mapperInterface) as IRqlMapper;
                if (mapper != null)
                    break;

                currentTypeFrom = currentTypeFrom.BaseType;
            }

            if (mapper == null)
                currentTypeFrom = k.Item1;

            // IRqlMapperContext is represented by an internal implementation that expects an IRqlMetadataProvider, 
            // hence it needs to be injected from the DI container
            var context = (RqlMapperContext)_serviceProvider.GetService(typeof(IRqlMapperContext<,>).MakeGenericType(currentTypeFrom!, typeTo))!;
            mapper?.MapEntity(context);
            context.AddMissing();

            return context.Mapping;
        });
    }
}
