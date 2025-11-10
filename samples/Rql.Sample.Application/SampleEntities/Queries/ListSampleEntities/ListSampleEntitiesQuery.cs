using MediatR;
using Rql.Sample.Domain.InMemory;

namespace Rql.Sample.Application.SampleEntities.Queries.ListSampleEntities;

public record ListSampleEntitiesQuery(string AccountId) : IRequest<IQueryable<SampleEntity>>;
