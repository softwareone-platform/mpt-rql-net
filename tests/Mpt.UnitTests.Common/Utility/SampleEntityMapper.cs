using Mpt.Rql;

namespace Mpt.UnitTests.Common.Utility;

internal class SampleEntityMapper<TView> : IRqlMapper<SampleEntity, TView> where TView : SampleEntityView, new()
{
    public void MapEntity(IRqlMapperContext<SampleEntity, TView> context)
    {
        context.MapStatic(t => t.Id, t => t.Id);
    }
}