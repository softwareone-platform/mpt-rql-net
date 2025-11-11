#pragma warning disable IDE0130
namespace Mpt.Rql.Client;

public interface IRqlRequestBuilderProvider
{
    IRqlRequestBuilder<T> GetBuilder<T>() where T : class;
}