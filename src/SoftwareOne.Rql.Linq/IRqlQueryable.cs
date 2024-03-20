#pragma warning disable IDE0130
namespace SoftwareOne.Rql;

public interface IRqlQueryable<in TStorage, TView>
{
    RqlResponse<TView> Transform(IQueryable<TStorage> source, RqlRequest request);
    RqlResponse<TView> Transform(IQueryable<TStorage> source, Action<RqlRequest> configure);
}

public interface IRqlQueryable<TStorage> : IRqlQueryable<TStorage, TStorage>
{
}