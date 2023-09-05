using System.Linq.Expressions;
#pragma warning disable IDE0130
namespace SoftwareOne.Rql.Client;

public interface IOrderBeginContext<T> : IOrderContext where T : class
{
    IThenByContext<T> OrderBy<TValue>(Expression<Func<T, TValue>> orderExpression);
    IThenByContext<T> OrderByDescending<TValue>(Expression<Func<T, TValue>> orderExpression);
}