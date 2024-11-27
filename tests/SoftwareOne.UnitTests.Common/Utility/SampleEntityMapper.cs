using SoftwareOne.Rql;

namespace SoftwareOne.UnitTests.Common.Utility;

internal class SampleEntityMapper<TView> : IRqlMapper<SampleEntity, TView> where TView : SampleEntityView, new()
{
    public void MapEntity(IRqlMapperContext<SampleEntity, TView> context)
    {
        context.Map(t => t.Id, t => t.Id);
    }
}