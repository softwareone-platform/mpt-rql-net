using MediatR;
using Rql.Sample.Domain.InMemory;

namespace Rql.Sample.Application.SampleEntities.Queries.ListSampleEntities
{
    public record ListSampleEntitiesQuery : IRequest<IQueryable<SampleEntity>>;
}
