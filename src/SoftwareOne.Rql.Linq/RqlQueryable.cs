using ErrorOr;
using Microsoft.Extensions.DependencyInjection;
using SoftwareOne.Rql.Linq.Configuration;
using SoftwareOne.Rql.Linq.Core;
using SoftwareOne.Rql.Linq.Services.Context;
using SoftwareOne.Rql.Linq.Services.Filtering;
using SoftwareOne.Rql.Linq.Services.Mapping;
using SoftwareOne.Rql.Linq.Services.Ordering;
using SoftwareOne.Rql.Linq.Services.Projection;
using System.Diagnostics;

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

    public RqlResponse<TView> Transform(IQueryable<TStorage> source, Action<RqlRequest> configure)
        => Transform(source, MakeRequest(configure));

    public RqlResponse<TView> Transform(IQueryable<TStorage> source, RqlRequest request)
        => TransformInternal(source, request);

    private RqlResponse<TView> TransformInternal(IQueryable<TStorage> source, RqlRequest request)
    {
        using var scope = _serviceProvider.CreateScope();

        var selectCustomization = request.Customization?.Select;
        if (selectCustomization == null)
        {
            var defaultSettings = GetService<IRqlDefaultSettings>();
            selectCustomization = defaultSettings.Select;
        }

        var context = GetService<IQueryContext<TView>>();

        GetService<IExternalServiceAccessor>().SetServiceProvider(_serviceProvider);
        GetService<IRqlSelectSettings>().Apply(selectCustomization);
        GetService<IFilteringService<TView>>().Process(request.Filter);
        GetService<IOrderingService<TView>>().Process(request.Order);
        GetService<IProjectionService<TView>>().Process(request.Select);
        GetService<IProjectionGraphBuilder<TView>>().BuildDefaults();

        var query = GetService<IMappingService<TStorage, TView>>().Apply(source);
        query = context.ApplyTransformations(query);

#if DEBUG
        Debug.Write(context.Graph.Print());
#endif

        return new RqlResponse<TView>
        {
            Graph = context.Graph,
            Query = query,
            Status = context.HasErrors ? context.GetErrors().ToList() : Result.Success
        };

        T GetService<T>() where T : notnull => scope.ServiceProvider.GetRequiredService<T>();
    }

    private static RqlRequest MakeRequest(Action<RqlRequest> configure)
    {
        var request = new RqlRequest();
        configure(request);
        return request;
    }
}


