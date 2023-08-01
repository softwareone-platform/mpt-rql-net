using System.Xml.Linq;
using System;

namespace SoftwareOne.Rql.Linq.Services.Filtering.Operators
{

    internal class OperatorHandlerProvider : IOperatorHandlerProvider
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IOperatorHandlerMaper _mapper;

        public OperatorHandlerProvider(IServiceProvider serviceProvider, IOperatorHandlerMaper mapper)
        {
            _serviceProvider = serviceProvider;
            _mapper = mapper;
        }

        public IOperator GetOperatorHandler(Type expression)
        {
            if (!_mapper.TryGetValue(expression, out var handlerType))
                throw new Exception($"Unknown expression key: {expression.Name}");

            return (IOperator)_serviceProvider.GetService(handlerType!)!;
        }
    }
}
