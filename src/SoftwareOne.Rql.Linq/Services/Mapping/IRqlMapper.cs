using System.Linq.Expressions;

#pragma warning disable IDE0130
namespace SoftwareOne.Rql;

public interface IRqlMapper
{
    void MapEntity(IRqlMapperContext context);
}

public interface IRqlMapper<TStorage, TView> : IRqlMapper
{
    void MapEntity(IRqlMapperContext<TStorage, TView> context);

    void IRqlMapper.MapEntity(IRqlMapperContext context) => MapEntity((IRqlMapperContext<TStorage, TView>)context);
}