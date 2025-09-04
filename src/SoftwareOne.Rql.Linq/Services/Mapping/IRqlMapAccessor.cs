using SoftwareOne.Rql.Abstractions.Mapping;
using SoftwareOne.Rql.Linq.Services.Mapping;

namespace SoftwareOne.Rql;

internal class RqlMapAccessor(IEntityMapCache mapCache) : IRqlMapAccessor
{
    public IEnumerable<IRqlMapEntry> GetEntries(Type typeFrom, Type typeTo)
        => GetMap(typeFrom, typeTo).Values;

    public Dictionary<string, IRqlMapEntry> GetMap(Type typeFrom, Type typeTo)
        => mapCache.Get(typeFrom, typeTo);
}
