using SoftwareOne.Rql.Linq.Services.Mapping;

#pragma warning disable IDE0130
namespace SoftwareOne.Rql;

public interface IRqlMapAccessor
{
    RqlMapDescriptor Get<TFrom, TTo>();
}

internal class RqlMapAccessor(IEntityMapCache mapCache) : IRqlMapAccessor
{
    public RqlMapDescriptor Get<TFrom, TTo>()
    {
        return new RqlMapDescriptor(mapCache.Get(typeof(TFrom), typeof(TTo)));
    }
}
