#pragma warning disable IDE0130
using Mpt.Rql.Linq.Client.Builder.Request;

namespace Mpt.Rql.Client;

public interface IRqlRequestBuilderProvider
{
    IRqlRequestBuilder<T> GetBuilder<T>() where T : class;
}