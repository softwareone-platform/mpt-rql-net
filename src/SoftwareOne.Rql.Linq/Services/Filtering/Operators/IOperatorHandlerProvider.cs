using SoftwareOne.Rql.Abstractions.Operators;

namespace SoftwareOne.Rql.Linq.Services.Filtering.Operators;

public interface IOperatorHandlerProvider
{
    IOperator GetOperatorHandler(Type expression);
}

