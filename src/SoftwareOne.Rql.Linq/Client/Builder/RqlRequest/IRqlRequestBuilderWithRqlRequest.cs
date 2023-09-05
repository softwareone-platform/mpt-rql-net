#pragma warning disable IDE0130
namespace SoftwareOne.Rql.Client;

public interface IRqlRequestBuilderWithRqlRequest<T>  : IRqlRequestBuilderCommon where T : class
{
    IRqlRequestBuilderWithOrder<T> Order(Func<IOrderBeginContext<T>, IOrderContext> orderFunc);
    IRqlRequestBuilderWithSelect<T> Select(Func<ISelectContext<T>, ISelectContext<T>> selectFunc);
}