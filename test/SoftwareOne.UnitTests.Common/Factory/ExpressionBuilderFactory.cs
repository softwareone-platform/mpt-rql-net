using Microsoft.Extensions.DependencyInjection;
using Moq;
using SoftwareOne.Rql.Abstractions;
using SoftwareOne.Rql.Linq.Services.Filtering;
using SoftwareOne.Rql.Linq.Services.Filtering.Builders;
using SoftwareOne.Rql.Linq.Services.Filtering.Operators;

namespace SoftwareOne.UnitTests.Common;

internal static class ExpressionBuilderFactory
{
    public static IExpressionBuilder GetBinary(IOperator operatorInstance)
    {
        return GetBuilder(sp =>
        new BinaryExpressionBuilder((IOperatorHandlerProvider)sp.GetService(typeof(IOperatorHandlerProvider))!,
        (IFilteringPathInfoBuilder)sp.GetService(typeof(IFilteringPathInfoBuilder))!), operatorInstance);
    }

    private static ExpressionBuilder GetBuilder<TNode>(Func<IServiceProvider, IConcreteExpressionBuilder<TNode>> builderCallback, IOperator operatorInstance)
        where TNode : RqlExpression
    {
        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider
            .Setup(x => x.GetService(typeof(IFilteringPathInfoBuilder)))
            .Returns(PathBuilderFactory.Internal);

        var operatorHandlerProviderMock = new Mock<IOperatorHandlerProvider>();
        operatorHandlerProviderMock.Setup(operatorHandlerProvider => operatorHandlerProvider.GetOperatorHandler(It.IsAny<Type>())).Returns(() =>
            operatorInstance);

        serviceProvider
            .Setup(x => x.GetService(typeof(IOperatorHandlerProvider)))
            .Returns(() => operatorHandlerProviderMock.Object);

        serviceProvider
            .Setup(x => x.GetService(typeof(IConcreteExpressionBuilder<TNode>)))
            .Returns(() => builderCallback(serviceProvider.Object));

        var serviceScope = new Mock<IServiceScope>();
        serviceScope.Setup(x => x.ServiceProvider).Returns(() => serviceProvider.Object);

        var serviceScopeFactory = new Mock<IServiceScopeFactory>();
        serviceScopeFactory
            .Setup(x => x.CreateScope())
            .Returns(() => serviceScope.Object);

        serviceProvider.Setup(x => x.GetService(typeof(IServiceScopeFactory))).Returns(serviceScopeFactory.Object);


        return new ExpressionBuilder(serviceProvider.Object);
    }
}