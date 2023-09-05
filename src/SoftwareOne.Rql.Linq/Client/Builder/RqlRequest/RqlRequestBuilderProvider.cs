using Microsoft.Extensions.DependencyInjection;
using SoftwareOne.Rql.Client;

namespace SoftwareOne.Rql.Linq.Client.RqlRequest;

internal class RqlRequestBuilderProvider : IRqlRequestBuilderProvider
{
    private readonly IServiceProvider _serviceProvider;
    public RqlRequestBuilderProvider(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IRqlRequestBuilder<T> GetRqlRequestBuilder<T>() where T : class 
        => _serviceProvider.GetRequiredService<IRqlRequestBuilder<T>>();
    
}