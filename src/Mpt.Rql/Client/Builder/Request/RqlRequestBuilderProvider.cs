using Microsoft.Extensions.DependencyInjection;

namespace Mpt.Rql.Client.Builder.Request;

internal class RqlRequestBuilderProvider : IRqlRequestBuilderProvider
{
    private readonly IServiceProvider _serviceProvider;
    public RqlRequestBuilderProvider(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IRqlRequestBuilder<T> GetBuilder<T>() where T : class
        => _serviceProvider.GetRequiredService<IRqlRequestBuilder<T>>();

}