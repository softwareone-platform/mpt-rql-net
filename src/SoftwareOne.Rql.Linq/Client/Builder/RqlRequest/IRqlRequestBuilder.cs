#pragma warning disable IDE0130
namespace SoftwareOne.Rql.Client;

public interface IRqlRequestBuilder<T> : IRqlRequestBuilderCommon where T : class
{
    IRqlRequestBuilderWithRqlRequest<T> Where(Func<IFilterContext<T>, IFilterContext<T>> filter);
    IRqlRequestBuilderWithOrder<T> Order(Func<IOrderBeginContext<T>, IOrderContext> order);
    IRqlRequestBuilderWithSelect<T> Select(Func<ISelectContext<T>, ISelectContext<T>> select);
}