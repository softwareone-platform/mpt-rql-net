using Rql.Sample.Domain.InMemory;

namespace Rql.Sample.Application.Common.Interfaces.Persistence.InMemory;

public interface ISampleRepository
{
    public IQueryable<SampleEntity> Query();
}
