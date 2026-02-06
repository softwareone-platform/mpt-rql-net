using Mpt.Rql;
using Rql.Sample.Contracts.InMemory;
using Rql.Sample.Domain.InMemory;

namespace Rql.Sample.Api.Mapping;

internal class SampleEntityMapper : IRqlMapper<SampleEntity, SampleEntityView>
{
    public void MapEntity(IRqlMapperContext<SampleEntity, SampleEntityView> context)
    {
        context.MapStatic(t => t.Id, t => t.Id);
    }
}
