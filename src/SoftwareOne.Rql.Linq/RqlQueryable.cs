using ErrorOr;
using Microsoft.Extensions.DependencyInjection;
using SoftwareOne.Rql.Linq.Services.Filtering;
using SoftwareOne.Rql.Linq.Services.Mapping;
using SoftwareOne.Rql.Linq.Services.Ordering;
using SoftwareOne.Rql.Linq.Services.Projection;

namespace SoftwareOne.Rql.Linq;

internal class RqlQueryable<TStorage> : RqlQueryableLinq<TStorage, TStorage>, IRqlQueryable<TStorage>
{
    public RqlQueryable(IServiceProvider serviceProvider) : base(serviceProvider) { }
}

internal class RqlQueryableLinq<TStorage, TView> : IRqlQueryable<TStorage, TView>
{
    private readonly IServiceProvider _serviceProvider;

    public RqlQueryableLinq(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public ErrorOr<IQueryable<TView>> Transform(IQueryable<TStorage> source, Action<RqlRequest> configure)
    {
        var request = new RqlRequest();
        configure(request);
        return Transform(source, request);
    }

    public ErrorOr<IQueryable<TView>> Transform(IQueryable<TStorage> source, RqlRequest request)
    {
        var errors = new List<Error>();
        using var scope = _serviceProvider.CreateScope();
        var queryResult = GetService<IMappingService<TStorage, TView>>(scope).Apply(source);

        if (queryResult.IsError)
            return queryResult.Errors;

        var query = queryResult.Value;

        GetService<IFilteringService<TView>>(scope).Apply(query, request.Filter).Switch(q => query = q, errors.AddRange);
        GetService<IOrderingService<TView>>(scope).Apply(query, request.Order).Switch(q => query = q, errors.AddRange);
        GetService<IProjectionService<TView>>(scope).Apply(query, request.Select).Switch(q => query = q, errors.AddRange);

        return errors.Any() ? errors : ErrorOrFactory.From(query);

        static T GetService<T>(IServiceScope scope) where T : notnull => scope.ServiceProvider.GetRequiredService<T>();
    }
}
