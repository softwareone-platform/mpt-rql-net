using Mpt.Rql.Core.Expressions;
using Mpt.Rql.Services.Filtering.Operators;
using System.Linq.Expressions;
using Xunit;

namespace Rql.Tests.Unit.Services;

public class StringExpressionHelperTests
{
    private readonly ParameterExpression _parameter = Expression.Parameter(typeof(TestObject), "x");
    private readonly Expression _memberExpression;

    public StringExpressionHelperTests()
    {
        _memberExpression = Expression.Property(_parameter, nameof(TestObject.Name));
    }

    [Theory]
    [InlineData("test", false)]
    [InlineData("Test", true)]
    [InlineData("TEST", true)]
    [InlineData("", false)]
    public void StartsWith_ShouldGenerateCorrectExpression(string searchValue, bool caseInsensitive)
    {
        // Act
        var expression = StringExpressionHelper.StartsWith(_memberExpression, searchValue, caseInsensitive);

        // Assert
        Assert.IsAssignableFrom<MethodCallExpression>(expression);
        var methodCall = (MethodCallExpression)expression;
        
        Assert.Equal("StartsWith", methodCall.Method.Name);
        Assert.Equal(_memberExpression, methodCall.Object);
        
        if (caseInsensitive)
        {
            // Should include StringComparison parameter for case insensitive
            Assert.True(methodCall.Arguments.Count >= 2);
            var lastArg = methodCall.Arguments[methodCall.Arguments.Count - 1];
            if (lastArg is ConstantExpression constantExpr && constantExpr.Value is StringComparison comparison)
            {
                Assert.Equal(StringComparison.OrdinalIgnoreCase, comparison);
            }
        }
    }

    [Theory]
    [InlineData("test", false)]
    [InlineData("Test", true)]
    [InlineData("TEST", true)]
    [InlineData("", false)]
    public void EndsWith_ShouldGenerateCorrectExpression(string searchValue, bool caseInsensitive)
    {
        // Act
        var expression = StringExpressionHelper.EndsWith(_memberExpression, searchValue, caseInsensitive);

        // Assert
        Assert.IsAssignableFrom<MethodCallExpression>(expression);
        var methodCall = (MethodCallExpression)expression;
        
        Assert.Equal("EndsWith", methodCall.Method.Name);
        Assert.Equal(_memberExpression, methodCall.Object);
        
        if (caseInsensitive)
        {
            // Should include StringComparison parameter for case insensitive
            Assert.True(methodCall.Arguments.Count >= 2);
            var lastArg = methodCall.Arguments[methodCall.Arguments.Count - 1];
            if (lastArg is ConstantExpression constantExpr && constantExpr.Value is StringComparison comparison)
            {
                Assert.Equal(StringComparison.OrdinalIgnoreCase, comparison);
            }
        }
    }

    [Theory]
    [InlineData("test", false)]
    [InlineData("Test", true)]
    [InlineData("TEST", true)]
    [InlineData("", false)]
    public void Contains_ShouldGenerateCorrectExpression(string searchValue, bool caseInsensitive)
    {
        // Act
        var expression = StringExpressionHelper.Contains(_memberExpression, searchValue, caseInsensitive);

        // Assert
        Assert.IsAssignableFrom<MethodCallExpression>(expression);
        var methodCall = (MethodCallExpression)expression;
        
        Assert.Equal("Contains", methodCall.Method.Name);
        Assert.Equal(_memberExpression, methodCall.Object);
        
        if (caseInsensitive)
        {
            // Should include StringComparison parameter for case insensitive
            Assert.True(methodCall.Arguments.Count >= 2);
            var lastArg = methodCall.Arguments[methodCall.Arguments.Count - 1];
            if (lastArg is ConstantExpression constantExpr && constantExpr.Value is StringComparison comparison)
            {
                Assert.Equal(StringComparison.OrdinalIgnoreCase, comparison);
            }
        }
    }

    [Theory]
    [InlineData("test", false)]
    [InlineData("Test", true)]
    [InlineData("TEST", true)]
    [InlineData("", false)]
    public void Equals_ShouldGenerateCorrectExpression(string searchValue, bool caseInsensitive)
    {
        // Act
        var expression = StringExpressionHelper.Equals(_memberExpression, searchValue, caseInsensitive);

        // Assert
        Assert.IsAssignableFrom<MethodCallExpression>(expression);
        var methodCall = (MethodCallExpression)expression;
        
        Assert.Equal("Equals", methodCall.Method.Name);
        Assert.Equal(_memberExpression, methodCall.Object);
        
        if (caseInsensitive)
        {
            // Should include StringComparison parameter for case insensitive
            Assert.True(methodCall.Arguments.Count >= 2);
            var lastArg = methodCall.Arguments[methodCall.Arguments.Count - 1];
            if (lastArg is ConstantExpression constantExpr && constantExpr.Value is StringComparison comparison)
            {
                Assert.Equal(StringComparison.OrdinalIgnoreCase, comparison);
            }
        }
    }

    [Theory]
    [InlineData("test", false)]
    [InlineData("Test", true)]
    [InlineData("TEST", true)]
    [InlineData("", false)]
    public void NotEquals_ShouldGenerateCorrectExpression(string searchValue, bool caseInsensitive)
    {
        // Act
        var expression = StringExpressionHelper.NotEquals(_memberExpression, searchValue, caseInsensitive);

        // Assert
        Assert.IsType<UnaryExpression>(expression);
        var unaryExpression = (UnaryExpression)expression;
        Assert.Equal(ExpressionType.Not, unaryExpression.NodeType);
        
        var methodCall = Assert.IsAssignableFrom<MethodCallExpression>(unaryExpression.Operand);
        Assert.Equal("Equals", methodCall.Method.Name);
        Assert.Equal(_memberExpression, methodCall.Object);
        
        if (caseInsensitive)
        {
            // Should include StringComparison parameter for case insensitive
            Assert.True(methodCall.Arguments.Count >= 2);
            var lastArg = methodCall.Arguments[methodCall.Arguments.Count - 1];
            if (lastArg is ConstantExpression constantExpr && constantExpr.Value is StringComparison comparison)
            {
                Assert.Equal(StringComparison.OrdinalIgnoreCase, comparison);
            }
        }
    }

    [Fact]
    public void AllMethods_ShouldCompileAndExecuteCorrectly()
    {
        // Arrange
        var testData = new[] { new TestObject { Name = "Test Value" } }.AsQueryable();

        // Test case-sensitive operations
        var startsWithExpr = Expression.Lambda<Func<TestObject, bool>>(
            StringExpressionHelper.StartsWith(_memberExpression, "Test", false), _parameter);
        var endsWithExpr = Expression.Lambda<Func<TestObject, bool>>(
            StringExpressionHelper.EndsWith(_memberExpression, "Value", false), _parameter);
        var containsExpr = Expression.Lambda<Func<TestObject, bool>>(
            StringExpressionHelper.Contains(_memberExpression, "st Val", false), _parameter);
        var equalsExpr = Expression.Lambda<Func<TestObject, bool>>(
            StringExpressionHelper.Equals(_memberExpression, "Test Value", false), _parameter);
        var notEqualsExpr = Expression.Lambda<Func<TestObject, bool>>(
            StringExpressionHelper.NotEquals(_memberExpression, "Wrong Value", false), _parameter);

        // Test case-insensitive operations
        var startsWithCIExpr = Expression.Lambda<Func<TestObject, bool>>(
            StringExpressionHelper.StartsWith(_memberExpression, "test", true), _parameter);
        var endsWithCIExpr = Expression.Lambda<Func<TestObject, bool>>(
            StringExpressionHelper.EndsWith(_memberExpression, "VALUE", true), _parameter);
        var containsCIExpr = Expression.Lambda<Func<TestObject, bool>>(
            StringExpressionHelper.Contains(_memberExpression, "ST VAL", true), _parameter);
        var equalsCIExpr = Expression.Lambda<Func<TestObject, bool>>(
            StringExpressionHelper.Equals(_memberExpression, "TEST VALUE", true), _parameter);
        var notEqualsCIExpr = Expression.Lambda<Func<TestObject, bool>>(
            StringExpressionHelper.NotEquals(_memberExpression, "WRONG VALUE", true), _parameter);

        // Act & Assert - All should return true (match the test data)
        Assert.Single(testData.Where(startsWithExpr.Compile()));
        Assert.Single(testData.Where(endsWithExpr.Compile()));
        Assert.Single(testData.Where(containsExpr.Compile()));
        Assert.Single(testData.Where(equalsExpr.Compile()));
        Assert.Single(testData.Where(notEqualsExpr.Compile()));

        // Case-insensitive should also match
        Assert.Single(testData.Where(startsWithCIExpr.Compile()));
        Assert.Single(testData.Where(endsWithCIExpr.Compile()));
        Assert.Single(testData.Where(containsCIExpr.Compile()));
        Assert.Single(testData.Where(equalsCIExpr.Compile()));
        Assert.Single(testData.Where(notEqualsCIExpr.Compile()));
    }

    private class TestObject
    {
        public string Name { get; set; } = string.Empty;
    }
}