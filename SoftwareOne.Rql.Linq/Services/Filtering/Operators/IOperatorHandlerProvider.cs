namespace SoftwareOne.Rql.Linq.Services.Filtering.Operators
{
    internal interface IOperatorHandlerProvider
    {
        IOperator GetOperatorHandler(Type expression);
    }
}
