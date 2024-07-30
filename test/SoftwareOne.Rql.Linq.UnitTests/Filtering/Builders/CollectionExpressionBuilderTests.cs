﻿using Moq;
using SoftwareOne.Rql.Abstractions;
using SoftwareOne.Rql.Abstractions.Argument;
using SoftwareOne.Rql.Abstractions.Collection;
using SoftwareOne.Rql.Linq.Core;
using SoftwareOne.Rql.Linq.Core.Result;
using SoftwareOne.Rql.Linq.Services.Filtering;
using SoftwareOne.Rql.Linq.Services.Filtering.Builders;
using SoftwareOne.Rql.Linq.Services.Filtering.Operators;
using SoftwareOne.Rql.Linq.Services.Filtering.Operators.Collection;
using System.Linq.Expressions;
using SoftwareOne.Rql.Linq.Services.Context;
using Xunit;

namespace SoftwareOne.Rql.Linq.UnitTests.Filtering.Builders;

public class CollectionExpressionBuilderTests
{
    private readonly Mock<IExpressionBuilder> _builderMock;
    private readonly Mock<IBuilderContext> _builderContextMock;
    private readonly Mock<IFilteringPathInfoBuilder> _pathBuilderMock;
    private readonly Mock<IOperatorHandlerProvider> _operatorHandlerProviderMock;
    private readonly ParameterExpression _pe;
    private readonly RqlCollection _node;
    private readonly CollectionExpressionBuilder _sut;
    private const string Path = "path";

    public CollectionExpressionBuilderTests()
    {
        _builderMock = new Mock<IExpressionBuilder>();
        _builderContextMock = new Mock<IBuilderContext>();
        _pathBuilderMock = new Mock<IFilteringPathInfoBuilder>();
        _operatorHandlerProviderMock = new Mock<IOperatorHandlerProvider>();
        var obj = new { SomeProperty = "" };
        _pe = Expression.Parameter(obj.GetType(), "x");
        _node = new RqlAny(new RqlConstant("collection"), new RqlConstant("id"));
        _sut = new CollectionExpressionBuilder(
            _builderContextMock.Object,
            _builderMock.Object,
            _operatorHandlerProviderMock.Object,
            _pathBuilderMock.Object);
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
        Assert.Equal("Collection operations work with properties only", result.Errors[0].Message);
    }

    [Fact]
    public void Build_WhenPropertyElementTypeIsNull_ShouldReturnError()
    {
        // Arrange
        var propertyInfo = new RqlPropertyInfo { ElementType = null };
        var pathInfo = new MemberPathInfo(Path, Path.AsMemory(0, 0), propertyInfo, Expression.Property(_pe, "SomeProperty"));
        _pathBuilderMock.Setup(pb => pb.Build(_pe, _node.Left)).Returns(pathInfo);

        // Act
        var result = _sut.Build(_pe, _node);

        // Assert
        Assert.True(result.IsError);
        Assert.Single(result.Errors);
        Assert.Equal("Collection property has incompatible type", result.Errors[0].Message);
    }

    [Fact]
    public void Build_WhenInnerExpressionIsError_ShouldReturnErrors()
    {
        // Arrange
        var propertyInfo = new RqlPropertyInfo { ElementType = typeof(object) };
        var pathInfo = new MemberPathInfo(Path, Path.AsMemory(0, 0), propertyInfo, Expression.Property(_pe, "SomeProperty"));
        _pathBuilderMock.Setup(pb => pb.Build(_pe, _node.Left)).Returns(pathInfo);

        var error = Error.General("Inner expression error");
        _builderMock.Setup(b => b.Build(It.IsAny<ParameterExpression>(), _node.Right!)).Returns(new List<Error> { error });

        // Act
        var result = _sut.Build(_pe, _node);

        // Assert
        Assert.True(result.IsError);
        Assert.Single(result.Errors);
        Assert.Equal(error, result.Errors[0]);
    }

    [Fact]
    public void Build_WhenAllValid_ShouldReturnExpression()
    {
        // Arrange
        var propertyInfo = new RqlPropertyInfo { ElementType = typeof(object) };
        var pathInfo = new MemberPathInfo(Path, Path.AsMemory(0, 0), propertyInfo, Expression.Property(_pe, "SomeProperty"));
        _pathBuilderMock.Setup(pb => pb.Build(_pe, _node.Left)).Returns(pathInfo);

        var innerExpression = Expression.Constant(true);
        _builderMock.Setup(b => b.Build(It.IsAny<ParameterExpression>(), _node.Right!)).Returns(innerExpression);

        var handlerMock = new Mock<ICollectionOperator>();
        handlerMock.Setup(h => h.MakeExpression(It.IsAny<IRqlPropertyInfo>(), It.IsAny<MemberExpression>(), It.IsAny<LambdaExpression>()))
            .Returns(Expression.Constant(true));
        _operatorHandlerProviderMock.Setup(op => op.GetOperatorHandler(It.IsAny<Type>())).Returns(handlerMock.Object);

        // Act
        var result = _sut.Build(_pe, _node);

        // Assert
        Assert.False(result.IsError);
        Assert.IsType<ConstantExpression>(result.Value);
    }
}