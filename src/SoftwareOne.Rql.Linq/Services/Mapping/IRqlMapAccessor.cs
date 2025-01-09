using SoftwareOne.Rql.Linq.Services.Mapping;

#pragma warning disable IDE0130
namespace SoftwareOne.Rql;

public interface IRqlMapAccessor
{
    RqlMapDescriptor Get<TFrom, TTo>();

    RqlMapDescriptor Get(Type typeFrom, Type typeTo);
}

internal class RqlMapAccessor(IEntityMapCache mapCache) : IRqlMapAccessor
{
    public RqlMapDescriptor Get<TFrom, TTo>()
        => Get(typeof(TFrom), typeof(TTo));

    public RqlMapDescriptor Get(Type typeFrom, Type typeTo)
        => new(mapCache.Get(typeFrom, typeTo));
}
