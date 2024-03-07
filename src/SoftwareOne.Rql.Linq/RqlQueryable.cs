using ErrorOr;
using Microsoft.Extensions.DependencyInjection;
using SoftwareOne.Rql.Linq.Configuration;
using SoftwareOne.Rql.Linq.Core;
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
        => Transform(source, MakeRequest(configure));

    public ErrorOr<IQueryable<TView>> Transform(IQueryable<TStorage> source, Action<RqlRequest> configure, out RqlAuditContext auditContext)
        => Transform(source, MakeRequest(configure), out auditContext);

    public ErrorOr<IQueryable<TView>> Transform(IQueryable<TStorage> source, RqlRequest request)
        => TransformInternal(source, request, null);

    public ErrorOr<IQueryable<TView>> Transform(IQueryable<TStorage> source, RqlRequest request, out RqlAuditContext auditContext)
    {
        auditContext = new RqlAuditContext();
        return TransformInternal(source, request, auditContext);
    }

    private ErrorOr<IQueryable<TView>> TransformInternal(IQueryable<TStorage> source, RqlRequest request, RqlAuditContext? auditContext)
    {
        var errors = new List<Error>();
        using var scope = _serviceProvider.CreateScope();

        if (auditContext != null)
            GetService<IAuditContextAccessor>(scope).SetContext(auditContext);

        var selectCustomization = request.Customization?.Select;
        if (selectCustomization == null)
        {
            var defaultSettings = GetService<IRqlDefaultSettings>(scope);
            selectCustomization = defaultSettings.Select;
        }

        GetService<IRqlSelectSettings>(scope).Apply(selectCustomization);

        var queryResult = GetService<IMappingService<TStorage, TView>>(scope).Apply(source);

        if (queryResult.IsError)
            return queryResult.Errors;

        var query = queryResult.Value;

        GetService<IFilteringService<TView>>(scope).Apply(query, request.Filter).Switch(q => query = q, errors.AddRange);
        GetService<IOrderingService<TView>>(scope).Apply(query, request.Order).Switch(q => query = q, errors.AddRange);
        GetService<IProjectionService<TView>>(scope).Apply(query, request.Select).Switch(q => query = q, errors.AddRange);

        return errors.Count != 0 ? errors : ErrorOrFactory.From(query);

        static T GetService<T>(IServiceScope scope) where T : notnull => scope.ServiceProvider.GetRequiredService<T>();
    }

    private static RqlRequest MakeRequest(Action<RqlRequest> configure)
    {
        var request = new RqlRequest();
        configure(request);
        return request;
    }
}
