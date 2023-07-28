using ErrorOr;

namespace SoftwareOne.Rql.Linq
{
    public interface IRqlQueryable<TStorage, TView>
    {
        ErrorOr<IQueryable<TView>> Transform(IQueryable<TStorage> source, RqlRequest request);
        ErrorOr<IQueryable<TView>> Transform(IQueryable<TStorage> source, Action<RqlRequest> configure);
    }

    public interface IRqlQueryable<TStorage> : IRqlQueryable<TStorage, TStorage>
    {
    }
}
