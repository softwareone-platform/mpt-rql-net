using MediatR;
using Rql.Sample.Application.Common.Interfaces.Persistence.InMemory;
using Rql.Sample.Domain.InMemory;

namespace Rql.Sample.Application.SampleEntities.Queries.ListSampleEntities;

public class ListSampleEntitiesHandler : IRequestHandler<ListSampleEntitiesQuery, IQueryable<SampleEntity>>
{
    private readonly ISampleRepository _repository;
    public ListSampleEntitiesHandler(ISampleRepository repository)
    {
        _repository = repository;
    }

    public Task<IQueryable<SampleEntity>> Handle(ListSampleEntitiesQuery request, CancellationToken cancellationToken)
        => Task.FromResult(_repository.Query());
}
