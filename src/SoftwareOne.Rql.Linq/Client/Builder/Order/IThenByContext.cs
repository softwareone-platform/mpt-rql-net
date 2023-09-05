using System.Linq.Expressions;
#pragma warning disable IDE0130
namespace SoftwareOne.Rql.Client;

public interface IThenByContext<T> : IOrderContext where T : class
{
    IThenByContext<T> ThenBy<TValue>(Expression<Func<T, TValue>> orderExpression);
    IThenByContext<T> ThenByDescending<TValue>(Expression<Func<T, TValue>> orderExpression);
}