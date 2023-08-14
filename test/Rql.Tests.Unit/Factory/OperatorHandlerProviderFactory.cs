using Moq;
using SoftwareOne.Rql.Linq.Services.Filtering.Operators;

namespace Rql.Tests.Unit.Factory;

internal static class OperatorHandlerProviderFactory
{
    internal static IOperatorHandlerProvider Equal()
    {
        var operatorHandlerProviderMock = new Mock<IOperatorHandlerProvider>();
        operatorHandlerProviderMock.Setup(operatorHandlerProvider => operatorHandlerProvider.GetOperatorHandler(It.IsAny<Type>())).Returns(
            new SoftwareOne.Rql.Linq.Services.Filtering.Operators.Comparison.Implementation.Equal());

        return operatorHandlerProviderMock.Object;
    }

    internal static IOperatorHandlerProvider NotEqual()
    {
        var operatorHandlerProviderMock = new Mock<IOperatorHandlerProvider>();
        operatorHandlerProviderMock.Setup(operatorHandlerProvider => operatorHandlerProvider.GetOperatorHandler(It.IsAny<Type>())).Returns(
            new SoftwareOne.Rql.Linq.Services.Filtering.Operators.Comparison.Implementation.NotEqual());

        return operatorHandlerProviderMock.Object;
    }

    internal static IOperatorHandlerProvider GreaterThan()
    {
        var operatorHandlerProviderMock = new Mock<IOperatorHandlerProvider>();
        operatorHandlerProviderMock.Setup(operatorHandlerProvider => operatorHandlerProvider.GetOperatorHandler(It.IsAny<Type>())).Returns(
            new SoftwareOne.Rql.Linq.Services.Filtering.Operators.Comparison.Implementation.GreaterThan());

        return operatorHandlerProviderMock.Object;
    }

    internal static IOperatorHandlerProvider GreaterEqualThan()
    {
        var operatorHandlerProviderMock = new Mock<IOperatorHandlerProvider>();
        operatorHandlerProviderMock.Setup(operatorHandlerProvider => operatorHandlerProvider.GetOperatorHandler(It.IsAny<Type>())).Returns(
            new SoftwareOne.Rql.Linq.Services.Filtering.Operators.Comparison.Implementation.GreaterThanOrEqual());

        return operatorHandlerProviderMock.Object;
    }

    internal static IOperatorHandlerProvider LessThan()
    {
        var operatorHandlerProviderMock = new Mock<IOperatorHandlerProvider>();
        operatorHandlerProviderMock.Setup(operatorHandlerProvider => operatorHandlerProvider.GetOperatorHandler(It.IsAny<Type>())).Returns(
            new SoftwareOne.Rql.Linq.Services.Filtering.Operators.Comparison.Implementation.LessThan());

        return operatorHandlerProviderMock.Object;
    }

    internal static IOperatorHandlerProvider LessEqualThan()
    {
        var operatorHandlerProviderMock = new Mock<IOperatorHandlerProvider>();
        operatorHandlerProviderMock.Setup(operatorHandlerProvider => operatorHandlerProvider.GetOperatorHandler(It.IsAny<Type>())).Returns(
            new SoftwareOne.Rql.Linq.Services.Filtering.Operators.Comparison.Implementation.LessThanOrEqual());

        return operatorHandlerProviderMock.Object;
    }

    internal static IOperatorHandlerProvider Like()
    {
        var operatorHandlerProviderMock = new Mock<IOperatorHandlerProvider>();
        operatorHandlerProviderMock.Setup(operatorHandlerProvider => operatorHandlerProvider.GetOperatorHandler(It.IsAny<Type>())).Returns(
            new SoftwareOne.Rql.Linq.Services.Filtering.Operators.Search.Implementation.Like());

        return operatorHandlerProviderMock.Object;
    }

    internal static IOperatorHandlerProvider ILike()
    {
        var operatorHandlerProviderMock = new Mock<IOperatorHandlerProvider>();
        operatorHandlerProviderMock.Setup(operatorHandlerProvider => operatorHandlerProvider.GetOperatorHandler(It.IsAny<Type>())).Returns(
            new SoftwareOne.Rql.Linq.Services.Filtering.Operators.Search.Implementation.LikeInsensitive());

        return operatorHandlerProviderMock.Object;
    }
}