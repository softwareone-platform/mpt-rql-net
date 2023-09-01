using ErrorOr;

#pragma warning disable IDE0130
namespace SoftwareOne.Rql;

public interface IRqlQueryable<in TStorage, TView>
{
    ErrorOr<IQueryable<TView>> Transform(IQueryable<TStorage> source, RqlRequest request);
    ErrorOr<IQueryable<TView>> Transform(IQueryable<TStorage> source, Action<RqlRequest> configure);
}

public interface IRqlQueryable<TStorage> : IRqlQueryable<TStorage, TStorage>
{
}