using ErrorOr;

#pragma warning disable IDE0130
namespace SoftwareOne.Rql;

public interface IRqlQueryable<in TStorage, TView>
{
    ErrorOr<IQueryable<TView>> Transform(IQueryable<TStorage> source, RqlRequest request);
    ErrorOr<IQueryable<TView>> Transform(IQueryable<TStorage> source, RqlRequest request, out RqlAuditContext auditContext);
    ErrorOr<IQueryable<TView>> Transform(IQueryable<TStorage> source, Action<RqlRequest> configure);
    ErrorOr<IQueryable<TView>> Transform(IQueryable<TStorage> source, Action<RqlRequest> configure, out RqlAuditContext auditContext);
}

public interface IRqlQueryable<TStorage> : IRqlQueryable<TStorage, TStorage>
{
}