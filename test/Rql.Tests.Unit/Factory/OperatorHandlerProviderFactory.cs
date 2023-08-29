using Moq;
using SoftwareOne.Rql.Linq.Services.Filtering.Operators;

namespace Rql.Tests.Unit.Factory;

internal static class OperatorHandlerProviderFactory
{
    internal static IOperatorHandlerProvider Equal()
        => Build<SoftwareOne.Rql.Linq.Services.Filtering.Operators.Comparison.Implementation.Equal>();

    internal static IOperatorHandlerProvider NotEqual()
        => Build<SoftwareOne.Rql.Linq.Services.Filtering.Operators.Comparison.Implementation.NotEqual>();

    internal static IOperatorHandlerProvider GreaterThan()
        => Build<SoftwareOne.Rql.Linq.Services.Filtering.Operators.Comparison.Implementation.GreaterThan>();

    internal static IOperatorHandlerProvider GreaterEqualThan()
        => Build<SoftwareOne.Rql.Linq.Services.Filtering.Operators.Comparison.Implementation.GreaterThanOrEqual>();

    internal static IOperatorHandlerProvider LessThan()
        => Build<SoftwareOne.Rql.Linq.Services.Filtering.Operators.Comparison.Implementation.LessThan>();

    internal static IOperatorHandlerProvider LessEqualThan()
         => Build<SoftwareOne.Rql.Linq.Services.Filtering.Operators.Comparison.Implementation.LessThanOrEqual>();

    internal static IOperatorHandlerProvider ListIn()
        => Build<SoftwareOne.Rql.Linq.Services.Filtering.Operators.List.Implementation.ListIn>();

    internal static IOperatorHandlerProvider ListOut()
        => Build<SoftwareOne.Rql.Linq.Services.Filtering.Operators.List.Implementation.ListOut>();

    internal static IOperatorHandlerProvider Like()
        => Build<SoftwareOne.Rql.Linq.Services.Filtering.Operators.Search.Implementation.Like>();

    internal static IOperatorHandlerProvider ILike()
        => Build<SoftwareOne.Rql.Linq.Services.Filtering.Operators.Search.Implementation.LikeInsensitive>();

    private static IOperatorHandlerProvider Build<T>() where T : IOperator, new()
    {
        var operatorHandlerProviderMock = new Mock<IOperatorHandlerProvider>();
        operatorHandlerProviderMock.Setup(operatorHandlerProvider => operatorHandlerProvider.GetOperatorHandler(It.IsAny<Type>())).Returns(
            new T());

        return operatorHandlerProviderMock.Object;
    }
}