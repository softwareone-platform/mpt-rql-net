using Rql.Sample.Contracts.InMemory;
using Rql.Sample.Domain.InMemory;
using SoftwareOne.Rql;

namespace Rql.Sample.Api.Mapping
{
    internal class SampleEntityMapper : IRqlMapper<SampleEntity, SampleEntityView>
    {
        public void MapEntity(IRqlMapperContext<SampleEntity, SampleEntityView> context)
        {
            context.MapStatic(t => t.Id, t => t.Id);
        }
    }
}
