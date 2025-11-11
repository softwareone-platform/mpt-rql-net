namespace Mpt.Rql.Core;

internal interface IExternalServiceAccessor
{
    void SetServiceProvider(IServiceProvider serviceProvider);
    object? GetService(Type type);
}

internal class ExternalServiceAccessor : IExternalServiceAccessor
{
    private IServiceProvider? _serviceProvider;

    public void SetServiceProvider(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public object? GetService(Type type)
    {
        return _serviceProvider!.GetService(type);
    }

}