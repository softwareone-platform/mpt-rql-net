using Mpt.Rql.Linq.Services.Mapping;

#pragma warning disable IDE0130
namespace Mpt.Rql;

public interface IRqlMapAccessor
{
    Dictionary<string, RqlMapEntry> GetMap<TFrom, TTo>()
        => GetMap(typeof(TFrom), typeof(TTo));

    Dictionary<string, RqlMapEntry> GetMap(Type typeFrom, Type typeTo);

    public IEnumerable<RqlMapEntry> GetEntries<TFrom, TTo>()
        => GetEntries(typeof(TFrom), typeof(TTo));

    public IEnumerable<RqlMapEntry> GetEntries(Type typeFrom, Type typeTo);
}

internal class RqlMapAccessor(IEntityMapCache mapCache) : IRqlMapAccessor
{
    public IEnumerable<RqlMapEntry> GetEntries(Type typeFrom, Type typeTo)
        => GetMap(typeFrom, typeTo).Values;

    public Dictionary<string, RqlMapEntry> GetMap(Type typeFrom, Type typeTo)
        => mapCache.Get(typeFrom, typeTo);
}
