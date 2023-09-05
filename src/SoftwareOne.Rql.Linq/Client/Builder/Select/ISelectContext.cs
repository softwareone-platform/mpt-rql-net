using System.Linq.Expressions;
#pragma warning disable IDE0130
namespace SoftwareOne.Rql.Client;

public interface ISelectContext<T> where T : class
{
    ISelectContext<T> Include<TValue>(Expression<Func<T, TValue>> exp);
    ISelectContext<T> Exclude<TValue>(Expression<Func<T, TValue>> exp);
}