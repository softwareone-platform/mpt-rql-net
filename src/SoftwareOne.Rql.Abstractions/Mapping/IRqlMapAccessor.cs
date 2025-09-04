namespace SoftwareOne.Rql.Abstractions.Mapping;

public interface IRqlMapAccessor
{
    Dictionary<string, IRqlMapEntry> GetMap<TFrom, TTo>()
        => GetMap(typeof(TFrom), typeof(TTo));

    Dictionary<string, IRqlMapEntry> GetMap(Type typeFrom, Type typeTo);

    public IEnumerable<IRqlMapEntry> GetEntries<TFrom, TTo>()
        => GetEntries(typeof(TFrom), typeof(TTo));

    public IEnumerable<IRqlMapEntry> GetEntries(Type typeFrom, Type typeTo);
}