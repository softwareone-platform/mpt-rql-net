using Moq;
using Mpt.Rql.Abstractions.Argument;
using Mpt.Rql.Abstractions.Binary;
using Mpt.Rql.Abstractions.Group;
using Mpt.Rql.Abstractions.Result;
using Mpt.Rql.Linq.Core;
using Mpt.Rql.Linq.Services.Filtering;
using Mpt.Rql.Linq.Services.Filtering.Builders;
using Mpt.Rql.Linq.Services.Filtering.Operators;
using Mpt.Rql.Linq.Services.Filtering.Operators.Comparison;
using Mpt.Rql.Linq.Services.Filtering.Operators.List;
using Mpt.Rql.Linq.Services.Filtering.Operators.Search;
using System.Linq.Expressions;
using Xunit;

namespace Mpt.Rql.Linq.UnitTests.Filtering.Builders;

public class BinaryExpressionBuilderTests
{
    private readonly Mock<IFilteringPathInfoBuilder> _pathBuilderMock;
    private readonly Mock<IOperatorHandlerProvider> _operatorHandlerProviderMock;
    private readonly ParameterExpression _pe;
    private readonly RqlBinary _node;
    private readonly BinaryExpressionBuilder _sut;
    private const string Path = "path";

    public BinaryExpressionBuilderTests()
    {
        _pathBuilderMock = new Mock<IFilteringPathInfoBuilder>();
        _operatorHandlerProviderMock = new Mock<IOperatorHandlerProvider>();
        var obj = new { SomeProperty = "" };
        _pe = Expression.Parameter(obj.GetType(), "x");
        _node = new RqlEqual(new RqlConstant("left"), new RqlConstant("right"));
        _sut = new BinaryExpressionBuilder(_operatorHandlerProviderMock.Object, _pathBuilderMock.Object);
    }

    [Fact]
    public void Build_WhenMemberInfoIsError_ShouldReturnErrors()
    {
        // Arrange
        var error = Error.General("Path error");
        _pathBuilderMock.Setup(pb => pb.Build(_pe, _node.Left)).Returns(error);

        // Act
        var result = _sut.Build(_pe, _node);

        // Assert
        Assert.True(result.IsError);
        Assert.Single(result.Errors);
        Assert.Equal(error, result.Errors[0]);
    }

    [Fact]
    public void Build_WhenAccessorIsNotMemberExpression_ShouldReturnError()
    {
        // Arrange
        var propertyInfo = new RqlPropertyInfo();
        var pathInfo = new MemberPathInfo(Path, Path.AsMemory(0, 0), propertyInfo, Expression.Constant(1));
        _pathBuilderMock.Setup(pb => pb.Build(_pe, _node.Left)).Returns(pathInfo);

        // Act
        var result = _sut.Build(_pe, _node);

        // Assert
        Assert.True(result.IsError);
        Assert.Single(result.Errors);
        Assert.Equal("Internal filtering error occurred. Please contact RQL package maintainer.", result.Errors[0].Message);
    }

    [Fact]
    public void Build_WhenHandlerIsComparisonOperatorAndRightConstantNotSupported_ShouldReturnError()
    {
        // Arrange
        var node = new RqlEqual(new RqlConstant("left"), new RqlAnd(new[] { new RqlConstant("exp1"), new RqlConstant("exp2") }));
        SetupPropertyInfo(node);
        var comparisonOperatorMock = new Mock<IComparisonOperator>();
        _operatorHandlerProviderMock.Setup(op => op.GetOperatorHandler(It.IsAny<Type>())).Returns(comparisonOperatorMock.Object);

        // Act
        var result = _sut.Build(_pe, node);

        // Assert
        Assert.True(result.IsError);
        Assert.Single(result.Errors);
        Assert.Equal("Unsupported argument type.", result.Errors[0].Message);
    }

    [Fact]
    public void Build_WhenHandlerIsComparisonOperator_ShouldInvokeMakeComparison()
    {
        // Arrange
        var propertyInfo = SetupPropertyInfo(_node);
        var comparisonOperatorMock = new Mock<IComparisonOperator>();
        comparisonOperatorMock.Setup(co => co.MakeExpression(propertyInfo, It.IsAny<Expression>(), It.IsAny<string>()))
            .Returns(Expression.Constant(true));
        _operatorHandlerProviderMock.Setup(op => op.GetOperatorHandler(It.IsAny<Type>())).Returns(comparisonOperatorMock.Object);

        // Act
        _sut.Build(_pe, _node);

        // Assert
        _operatorHandlerProviderMock.Verify(op => op.GetOperatorHandler(It.IsAny<Type>()), Times.Once);
        comparisonOperatorMock.Verify(comp => comp.MakeExpression(propertyInfo, It.IsAny<Expression>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public void Build_WhenHandlerIsSearchOperatorAndRightConstantNull_ShouldReturnError()
    {
        // Arrange
        var node = new RqlEqual(new RqlConstant("left"), new RqlConstant(null!));
        SetupPropertyInfo(node);
        var searchOperatorMock = new Mock<ISearchOperator>();
        _operatorHandlerProviderMock.Setup(op => op.GetOperatorHandler(It.IsAny<Type>())).Returns(searchOperatorMock.Object);

        // Act
        var result = _sut.Build(_pe, node);

        // Assert
        Assert.True(result.IsError);
        Assert.Single(result.Errors);
        Assert.Equal("Null values are not supported.", result.Errors[0].Message);
    }

    [Fact]
    public void Build_WhenHandlerIsSearchOperator_ShouldInvokeMakeSearch()
    {
        // Arrange
        var propertyInfo = SetupPropertyInfo(_node);
        var searchOperatorMock = new Mock<ISearchOperator>();
        searchOperatorMock.Setup(so => so.MakeExpression(propertyInfo, It.IsAny<MemberExpression>(), It.IsAny<string>()))
            .Returns(Expression.Constant(true));
        _operatorHandlerProviderMock.Setup(op => op.GetOperatorHandler(It.IsAny<Type>())).Returns(searchOperatorMock.Object);

        // Act
        _sut.Build(_pe, _node);

        // Assert
        _operatorHandlerProviderMock.Verify(op => op.GetOperatorHandler(It.IsAny<Type>()), Times.Once);
        searchOperatorMock.Verify(search => search.MakeExpression(propertyInfo, It.IsAny<MemberExpression>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public void Build_WhenHandlerIsListOperator_ShouldInvokeMakeList()
    {
        // Arrange
        var node = new RqlEqual(new RqlConstant("left"), new RqlAnd(new[] { new RqlConstant("exp1"), new RqlConstant("exp2") }));
        var propertyInfo = SetupPropertyInfo(node);
        var listOperatorMock = new Mock<IListOperator>();
        listOperatorMock
            .Setup(lo => lo.MakeExpression(propertyInfo, It.IsAny<MemberExpression>(), It.IsAny<IEnumerable<string>>()))
            .Returns(Expression.Constant(true));
        _operatorHandlerProviderMock.Setup(op => op.GetOperatorHandler(It.IsAny<Type>())).Returns(listOperatorMock.Object);


        // Act
        _sut.Build(_pe, node);

        // Assert
        _operatorHandlerProviderMock.Verify(op => op.GetOperatorHandler(It.IsAny<Type>()), Times.Once);
        listOperatorMock.Verify(list => list.MakeExpression(propertyInfo, It.IsAny<MemberExpression>(), It.IsAny<IEnumerable<string>>()), Times.Once);
    }

    private RqlPropertyInfo SetupPropertyInfo(RqlBinary node)
    {
        var propertyInfo = new RqlPropertyInfo { ElementType = typeof(object) };
        var pathInfo = new MemberPathInfo(Path, Path.AsMemory(0, 0), propertyInfo, Expression.Property(_pe, "SomeProperty"));
        _pathBuilderMock.Setup(pb => pb.Build(_pe, node.Left)).Returns(pathInfo);
        return propertyInfo;
    }
}