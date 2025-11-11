using Microsoft.Extensions.DependencyInjection;
using Mpt.Rql.Abstractions.Configuration;
using Mpt.Rql.Core;
using Mpt.Rql.Services.Context;
using Mpt.Rql.Services.Filtering;
using Mpt.Rql.Services.Mapping;
using Mpt.Rql.Services.Ordering;
using Mpt.Rql.Services.Projection;
using Mpt.Rql.Settings;

namespace Mpt.Rql;

internal class RqlQueryable<TStorage>(IServiceProvider serviceProvider) : RqlQueryableLinq<TStorage, TStorage>(serviceProvider), IRqlQueryable<TStorage>
{
}

internal class RqlQueryableLinq<TStorage, TView> : IRqlQueryable<TStorage, TView>
{
    private readonly IServiceProvider _serviceProvider;

    public RqlQueryableLinq(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public RqlGraphResponse BuildGraph(RqlRequest request)
        => BuildGraph(request, static _ => { });

    public RqlGraphResponse BuildGraph(RqlRequest request, Action<IRqlSettings> configure)
       => TransformInternal(null!, request, configure, true);

    public RqlResponse<TView> Transform(IQueryable<TStorage> source, RqlRequest request)
        => Transform(source, request, static _ => { });

    public RqlResponse<TView> Transform(IQueryable<TStorage> source, RqlRequest request, Action<IRqlSettings> configure)
        => TransformInternal(source, request, configure, false);

    private RqlResponse<TView> TransformInternal(IQueryable<TStorage> source, RqlRequest request, Action<IRqlSettings> configure, bool skipTransformStage)
    {
        using var scope = _serviceProvider.CreateScope();
        var settingsAccessor = GetService<IRqlSettingsAccessor>();
        configure(settingsAccessor.Current);

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
            Errors = [.. context.GetErrors()]
        };

        T GetService<T>() where T : notnull => scope.ServiceProvider.GetRequiredService<T>();
    }
}


