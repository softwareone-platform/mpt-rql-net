namespace Mpt.Rql.Services.Filtering.Operators;

public interface IOperatorHandlerProvider
{
    IOperator GetOperatorHandler(Type expression);
}

