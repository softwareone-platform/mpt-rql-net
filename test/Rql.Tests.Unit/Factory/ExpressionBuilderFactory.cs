using Microsoft.Extensions.DependencyInjection;
using Moq;
using SoftwareOne.Rql.Abstractions;
using SoftwareOne.Rql.Abstractions.Binary;
using SoftwareOne.Rql.Linq.Services.Filtering;
using SoftwareOne.Rql.Linq.Services.Filtering.Builders;
using SoftwareOne.Rql.Linq.Services.Filtering.Operators;

namespace Rql.Tests.Unit.Factory;

internal static class ExpressionBuilderFactory
{
    public static IExpressionBuilder GetBinary<TOperator>()
        where TOperator : IOperator, new()
    {
        return GetBuilder<RqlBinary, TOperator>(sp =>
        new BinaryExpressionBuilder((IOperatorHandlerProvider)sp.GetService(typeof(IOperatorHandlerProvider))!, (IFilteringPathInfoBuilder)sp.GetService(typeof(IFilteringPathInfoBuilder))!));
    }

    private static IExpressionBuilder GetBuilder<TNode, TOperator>(Func<IServiceProvider, IConcreteExpressionBuilder<TNode>> builderCallback) where TNode : RqlExpression
        where TOperator : IOperator, new()
    {
        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider
            .Setup(x => x.GetService(typeof(IFilteringPathInfoBuilder)))
            .Returns(PathBuilderFactory.Internal);

        var operatorHandlerProviderMock = new Mock<IOperatorHandlerProvider>();
        operatorHandlerProviderMock.Setup(operatorHandlerProvider => operatorHandlerProvider.GetOperatorHandler(It.IsAny<Type>())).Returns(() =>
            new TOperator());

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