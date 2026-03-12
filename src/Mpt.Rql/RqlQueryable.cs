using Microsoft.Extensions.DependencyInjection;
using Mpt.Rql.Abstractions.Configuration;
using Mpt.Rql.Abstractions.Result;
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

    protected static readonly List<Error> EmptyErrors = [];

    public RqlQueryableLinq(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public RqlGraphResponse BuildGraph(RqlRequest request)
        => BuildGraph(request, static _ => { });

    public RqlGraphResponse BuildGraph(RqlRequest request, Action<IRqlSettings> configure)
       => TransformInternal(null!, request, configure, skipTransformStage: true);

    public RqlResponse<TView> Transform(IQueryable<TStorage> source, RqlRequest request)
        => Transform(source, request, static _ => { });

    public RqlResponse<TView> Transform(IQueryable<TStorage> source, RqlRequest request, Action<IRqlSettings> configure)
        => TransformInternal(source, request, configure, skipTransformStage: false);

    protected virtual RqlResponse<TView> TransformInternal(
        IQueryable<TStorage> source,
        RqlRequest request,
        Action<IRqlSettings> configure,
        bool skipTransformStage)
    {
        using var scope = _serviceProvider.CreateScope();
        var services = ResolveServices(scope.ServiceProvider);
        services.ExternalServiceAccessor.SetServiceProvider(_serviceProvider);
        return ExecutePipeline(source, request, configure, skipTransformStage, services);
    }

    protected static ResolvedServices ResolveServices(IServiceProvider scopedProvider)
    {
        return new ResolvedServices
        {
            ScopedProvider = scopedProvider,
            SettingsAccessor = scopedProvider.GetRequiredService<IRqlSettingsAccessor>(),
            ExternalServiceAccessor = scopedProvider.GetRequiredService<IExternalServiceAccessor>(),
            Context = scopedProvider.GetRequiredService<IQueryContext<TView>>(),
            FilteringService = scopedProvider.GetRequiredService<IFilteringService<TView>>(),
            OrderingService = scopedProvider.GetRequiredService<IOrderingService<TView>>(),
            ProjectionService = scopedProvider.GetRequiredService<IProjectionService<TView>>(),
            GraphBuilder = scopedProvider.GetRequiredService<IProjectionGraphBuilder<TView>>(),
        };
    }

    protected static RqlResponse<TView> ExecutePipeline(
        IQueryable<TStorage> source,
        RqlRequest request,
        Action<IRqlSettings> configure,
        bool skipTransformStage,
        ResolvedServices services)
    {
        configure(services.SettingsAccessor.Current);

        services.FilteringService.Process(request.Filter);
        services.OrderingService.Process(request.Order);
        services.ProjectionService.Process(request.Select);
        services.GraphBuilder.BuildDefaults();

        IQueryable<TView>? query = null;
        if (!skipTransformStage)
        {
            if (services.SettingsAccessor.Current.Mapping.Transparent && typeof(TView) == typeof(TStorage))
                query = (IQueryable<TView>)source;
            else
                query = services.ScopedProvider.GetRequiredService<IMappingService<TStorage, TView>>().Apply(source);
            

            query = services.Context.ApplyTransformations(query);
        }

        return new RqlResponse<TView>
        {
            Graph = services.Context.Graph,
            Query = query!,
            IsSuccess = !services.Context.HasErrors,
            Errors = services.Context.HasErrors ? [.. services.Context.GetErrors()] : EmptyErrors
        };
    }


    protected readonly struct ResolvedServices
    {
        public required IServiceProvider ScopedProvider { get; init; }
        public required IRqlSettingsAccessor SettingsAccessor { get; init; }
        public required IExternalServiceAccessor ExternalServiceAccessor { get; init; }
        public required IQueryContext<TView> Context { get; init; }
        public required IFilteringService<TView> FilteringService { get; init; }
        public required IOrderingService<TView> OrderingService { get; init; }
        public required IProjectionService<TView> ProjectionService { get; init; }
        public required IProjectionGraphBuilder<TView> GraphBuilder { get; init; }
    }
}