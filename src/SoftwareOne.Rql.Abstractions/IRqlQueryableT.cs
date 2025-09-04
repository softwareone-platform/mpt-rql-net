#pragma warning disable IDE0130
namespace SoftwareOne.Rql;

public interface IRqlQueryable<TStorage> : IRqlQueryable<TStorage, TStorage>
{
}