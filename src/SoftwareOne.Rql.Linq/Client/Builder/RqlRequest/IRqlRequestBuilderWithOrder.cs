#pragma warning disable IDE0130
namespace SoftwareOne.Rql.Client;

public interface IRqlRequestBuilderWithOrder<T>  : IRqlRequestBuilderCommon where T : class
{
    IRqlRequestBuilderWithSelect<T> Select(Func<ISelectContext<T>, ISelectContext<T>> selectFunc);
}