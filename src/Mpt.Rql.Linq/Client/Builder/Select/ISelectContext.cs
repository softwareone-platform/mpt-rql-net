using System.Linq.Expressions;
#pragma warning disable IDE0130
namespace Mpt.Rql.Client;

public interface ISelectContext<T> where T : class
{
    ISelectContext<T> Include(params Expression<Func<T, object>>[] exp);
    ISelectContext<T> Exclude(params Expression<Func<T, object>>[] exp);
}