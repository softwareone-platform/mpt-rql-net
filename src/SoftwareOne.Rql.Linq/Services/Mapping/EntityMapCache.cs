using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace SoftwareOne.Rql.Linq.Services.Mapping
{
    internal interface IEntityMapCache
    {
        Dictionary<string, (LambdaExpression Expression, bool IsDynamic)> Get(Type typeFrom, Type typeTo);
    }

    internal class EntityMapCache : IEntityMapCache
    {
        private static readonly ConcurrentDictionary<(Type, Type), Dictionary<string, (LambdaExpression Expression, bool IsDynamic)>> _cache = [];

        private readonly IServiceProvider _serviceProvider;


        public EntityMapCache(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public Dictionary<string, (LambdaExpression Expression, bool IsDynamic)> Get(Type typeFrom, Type typeTo)
        {
            var key = (typeFrom, typeTo);
            return _cache.GetOrAdd(key, k =>
            {
                IRqlMapper? mapper = null;

                var currentTypeFrom = k.Item1;
                var typeTo = k.Item2;

                while (currentTypeFrom != null)
                {
                    var mapperType = typeof(IRqlMapper<,>).MakeGenericType(currentTypeFrom, typeTo);
                    mapper = _serviceProvider.GetService(mapperType) as IRqlMapper;
                    if (mapper != null)
                        break;

                    currentTypeFrom = currentTypeFrom.BaseType;
                }

                if (mapper == null)
                    currentTypeFrom = k.Item1;

                var context = (IRqlMapperContext)_serviceProvider.GetService(typeof(IRqlMapperContext<,>).MakeGenericType(currentTypeFrom!, typeTo))!;
                mapper?.MapEntity(context);
                context.AddMissing();

                var result = new Dictionary<string, (LambdaExpression Expression, bool IsDynamic)>(context.Mapping.Count);
                foreach (var mapEntry in context.Mapping)
                {
                    var fromExp = mapEntry.Value;
                    if (!result.TryAdd(mapEntry.Key, fromExp))
                        result[mapEntry.Key] = fromExp;
                }

                return result;
            });
        }
    }
}
