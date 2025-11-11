namespace Mpt.Rql.Linq.Services.Filtering.Operators;

internal class OperatorHandlerProvider : IOperatorHandlerProvider
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IOperatorHandlerMapper _mapper;

    public OperatorHandlerProvider(IServiceProvider serviceProvider, IOperatorHandlerMapper mapper)
    {
        _serviceProvider = serviceProvider;
        _mapper = mapper;
    }

    public IOperator GetOperatorHandler(Type expression)
    {
        if (!_mapper.TryGetValue(expression, out var handlerType))
            throw new NotImplementedException($"Expression key is not implemented: {expression.Name}");

        return (IOperator)_serviceProvider.GetService(handlerType!)!;
    }
}
