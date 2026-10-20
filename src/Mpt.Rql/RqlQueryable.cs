using Microsoft.Extensions.DependencyInjection;
using Mpt.Rql.Abstractions.Configuration;
using Mpt.Rql.Abstractions.Configuration.Filter;
using Mpt.Rql.Core;
using Mpt.Rql.Services.Context;
using Mpt.Rql.Services.Filtering;
using Mpt.Rql.Services.Filtering.Binding;
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

    public RqlGraphResponse BuildGraph(RqlRequest request, Action<IRqlTransformOptions> configure)
       => TransformInternal(null!, request, configure, true);

    public RqlResponse<TView> Transform(IQueryable<TStorage> source, RqlRequest request)
        => Transform(source, request, static _ => { });

    public RqlResponse<TView> Transform(IQueryable<TStorage> source, RqlRequest request, Action<IRqlTransformOptions> configure)
        => TransformInternal(source, request, configure, false);

    private RqlResponse<TView> TransformInternal(IQueryable<TStorage> source, RqlRequest request, Action<IRqlTransformOptions> configure, bool skipTransformStage)
    {
        using var scope = _serviceProvider.CreateScope();
        var settingsAccessor = GetService<IRqlSettingsAccessor>();
        configure(GetService<RqlTransformOptions>());

        GetService<IExternalServiceAccessor>().SetServiceProvider(_serviceProvider);

        var context = GetService<IQueryContext<TView>>();
        var projectionGraphBuilder = GetService<IProjectionGraphBuilder<TView>>();

        projectionGraphBuilder.BuildDecisions();
        GetService<IFilteringService<TView>>().Process(request.Filter);
        GetService<IOrderingService<TView>>().Process(request.Order);
        GetService<IProjectionService<TView>>().Process(request.Select);
        projectionGraphBuilder.BuildDefaults();

        IQueryable<TView>? query = null;
        if (!skipTransformStage)
        {
            var settings = settingsAccessor.Current;
            var filterOnView = true;

            if (settings.Mapping.Transparent && typeof(TView) == typeof(TStorage))
            {
                query = (IQueryable<TView>)source;
            }
            else
            {
                var selector = GetService<IMappingService<TStorage, TView>>().BuildSelector();

                // Storage binding filters before the projection, so the projection's null guards become
                // boolean terms instead of conditionals around the compared member (see FilterBinding).
                if (context.FilterPredicate != null && settings.Filter.Binding == FilterBinding.Storage)
                {
                    source = source.Where(StoragePredicateBinder.Bind(context.FilterPredicate, selector));
                    filterOnView = false;
                }

                query = source.Select(selector);
            }

            query = context.ApplyTransformations(query, filterOnView);
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


