#pragma warning disable IDE0130
namespace SoftwareOne.Rql.Client;

public interface IRqlRequestBuilderProvider
{
    IRqlRequestBuilder<T> GetRqlRequestBuilder<T>() where T : class;
}