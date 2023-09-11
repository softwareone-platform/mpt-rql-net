#pragma warning disable IDE0130
using SoftwareOne.Rql.Linq.Client.Builder.Request;

namespace SoftwareOne.Rql.Client;

public interface IRqlRequestBuilderProvider
{
    IRqlRequestBuilder<T> GetBuilder<T>() where T : class;
}