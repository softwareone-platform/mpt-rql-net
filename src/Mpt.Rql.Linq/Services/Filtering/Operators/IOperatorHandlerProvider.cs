namespace Mpt.Rql.Linq.Services.Filtering.Operators;

public interface IOperatorHandlerProvider
{
    IOperator GetOperatorHandler(Type expression);
}

