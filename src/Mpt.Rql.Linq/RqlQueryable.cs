using Microsoft.Extensions.DependencyInjection;
using Mpt.Rql.Linq.Configuration;
using Mpt.Rql.Linq.Core;
using Mpt.Rql.Linq.Services.Context;
using Mpt.Rql.Linq.Services.Filtering;
using Mpt.Rql.Linq.Services.Mapping;
using Mpt.Rql.Linq.Services.Ordering;
using Mpt.Rql.Linq.Services.Projection;

namespace Mpt.Rql.Linq;

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

    public RqlGraphResponse BuildGraph(RqlRequest request)
        => TransformInternal(null!, request, true);

    public RqlResponse<TView> Transform(IQueryable<TStorage> source, Action<RqlRequest> configure)
        => Transform(source, MakeRequest(configure));

    public RqlResponse<TView> Transform(IQueryable<TStorage> source, RqlRequest request)
        => TransformInternal(source, request, false);

    private RqlResponse<TView> TransformInternal(IQueryable<TStorage> source, RqlRequest request, bool skipTransformStage)
    {
        using var scope = _serviceProvider.CreateScope();
        var settingsAccessor = GetService<IRqlSettingsAccessor>();
        settingsAccessor.Override(request.Settings);

        var context = GetService<IQueryContext<TView>>();

        GetService<IExternalServiceAccessor>().SetServiceProvider(_serviceProvider);
        GetService<IFilteringService<TView>>().Process(request.Filter);
        GetService<IOrderingService<TView>>().Process(request.Order);
        GetService<IProjectionService<TView>>().Process(request.Select);
        GetService<IProjectionGraphBuilder<TView>>().BuildDefaults();

        IQueryable<TView>? query = null;
        if (!skipTransformStage)
        {
            if (settingsAccessor.Current.Mapping.Transparent && typeof(TView) == typeof(TStorage))
                query = (IQueryable<TView>)source;
            else
                query = GetService<IMappingService<TStorage, TView>>().Apply(source);

            query = context.ApplyTransformations(query);
        }

        return new RqlResponse<TView>
        {
            Graph = context.Graph,
            Query = query!,
            IsSuccess = !context.HasErrors,
            Errors = context.GetErrors().ToList()
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


