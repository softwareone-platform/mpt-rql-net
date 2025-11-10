#pragma warning disable IDE0130
namespace Mpt.Rql;

public interface IRqlQueryable<TStorage> : IRqlQueryable<TStorage, TStorage>
{
}