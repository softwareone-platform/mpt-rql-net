using Mpt.Rql.Abstractions.Configuration;
using Mpt.Rql.Abstractions.Configuration.Filter;
using Mpt.Rql.Core.Expressions;
using Mpt.Rql.Services.Filtering.Operators;
using Mpt.Rql.Settings;
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

    private static IRqlSettings CreateSettings(StringComparison? comparison = null, SafeNavigationMode safeNavigation = SafeNavigationMode.Off)
    {
        var settings = new RqlSettings();
        if (comparison.HasValue)
            settings.Filter.Strings.Comparison = comparison.Value;
        settings.Filter.SafeNavigation = safeNavigation;
        
        return settings;
    }

    [Theory]
    [InlineData("test", null)]
    [InlineData("Test", StringComparison.OrdinalIgnoreCase)]
    [InlineData("TEST", StringComparison.OrdinalIgnoreCase)]
    [InlineData("", null)]
    public void StartsWith_ShouldGenerateCorrectExpression(string searchValue, StringComparison? comparison)
    {
        // Arrange
        var settings = CreateSettings(comparison);
        
        // Act
        var expression = _memberExpression.StartsWith(searchValue, settings);

        // Assert
        Assert.IsAssignableFrom<MethodCallExpression>(expression);
        var methodCall = (MethodCallExpression)expression;
        
        Assert.Equal("StartsWith", methodCall.Method.Name);
        Assert.Equal(_memberExpression, methodCall.Object);
        
        if (comparison.HasValue)
        {
            // Should include StringComparison parameter for case insensitive
            Assert.True(methodCall.Arguments.Count >= 2);
            var lastArg = methodCall.Arguments[methodCall.Arguments.Count - 1];
            if (lastArg is ConstantExpression constantExpr && constantExpr.Value is StringComparison actualComparison)
            {
                Assert.Equal(comparison.Value, actualComparison);
            }
        }
    }

    [Theory]
    [InlineData("test", null)]
    [InlineData("Test", StringComparison.OrdinalIgnoreCase)]
    [InlineData("TEST", StringComparison.OrdinalIgnoreCase)]
    [InlineData("", null)]
    public void EndsWith_ShouldGenerateCorrectExpression(string searchValue, StringComparison? comparison)
    {
        // Arrange
        var settings = CreateSettings(comparison);
        
        // Act
        var expression = _memberExpression.EndsWith(searchValue, settings);

        // Assert
        Assert.IsAssignableFrom<MethodCallExpression>(expression);
        var methodCall = (MethodCallExpression)expression;
        
        Assert.Equal("EndsWith", methodCall.Method.Name);
        Assert.Equal(_memberExpression, methodCall.Object);
        
        if (comparison.HasValue)
        {
            // Should include StringComparison parameter for case insensitive
            Assert.True(methodCall.Arguments.Count >= 2);
            var lastArg = methodCall.Arguments[methodCall.Arguments.Count - 1];
            if (lastArg is ConstantExpression constantExpr && constantExpr.Value is StringComparison actualComparison)
            {
                Assert.Equal(comparison.Value, actualComparison);
            }
        }
    }

    [Theory]
    [InlineData("test", null)]
    [InlineData("Test", StringComparison.OrdinalIgnoreCase)]
    [InlineData("TEST", StringComparison.OrdinalIgnoreCase)]
    [InlineData("", null)]
    public void Contains_ShouldGenerateCorrectExpression(string searchValue, StringComparison? comparison)
    {
        // Arrange
        var settings = CreateSettings(comparison);
        
        // Act
        var expression = _memberExpression.Contains(searchValue, settings);

        // Assert
        Assert.IsAssignableFrom<MethodCallExpression>(expression);
        var methodCall = (MethodCallExpression)expression;
        
        Assert.Equal("Contains", methodCall.Method.Name);
        Assert.Equal(_memberExpression, methodCall.Object);
        
        if (comparison.HasValue)
        {
            // Should include StringComparison parameter for case insensitive
            Assert.True(methodCall.Arguments.Count >= 2);
            var lastArg = methodCall.Arguments[methodCall.Arguments.Count - 1];
            if (lastArg is ConstantExpression constantExpr && constantExpr.Value is StringComparison actualComparison)
            {
                Assert.Equal(comparison.Value, actualComparison);
            }
        }
    }

    [Theory]
    [InlineData("test", null)]
    [InlineData("Test", StringComparison.OrdinalIgnoreCase)]
    [InlineData("TEST", StringComparison.OrdinalIgnoreCase)]
    [InlineData("", null)]
    public void Equals_ShouldGenerateCorrectExpression(string searchValue, StringComparison? comparison)
    {
        // Arrange
        var settings = CreateSettings(comparison);
        
        // Act
        var expression = _memberExpression.Equals(searchValue, settings);

        // Assert
        Assert.IsAssignableFrom<MethodCallExpression>(expression);
        var methodCall = (MethodCallExpression)expression;
        
        Assert.Equal("Equals", methodCall.Method.Name);
        Assert.Equal(_memberExpression, methodCall.Object);
        
        if (comparison.HasValue)
        {
            // Should include StringComparison parameter for case insensitive
            Assert.True(methodCall.Arguments.Count >= 2);
            var lastArg = methodCall.Arguments[methodCall.Arguments.Count - 1];
            if (lastArg is ConstantExpression constantExpr && constantExpr.Value is StringComparison actualComparison)
            {
                Assert.Equal(comparison.Value, actualComparison);
            }
        }
    }

    [Theory]
    [InlineData("test", null)]
    [InlineData("Test", StringComparison.OrdinalIgnoreCase)]
    [InlineData("TEST", StringComparison.OrdinalIgnoreCase)]
    [InlineData("", null)]
    public void NotEquals_ShouldGenerateCorrectExpression(string searchValue, StringComparison? comparison)
    {
        // Arrange
        var settings = CreateSettings(comparison);
        
        // Act
        var expression = _memberExpression.NotEquals(searchValue, settings);

        // Assert
        Assert.IsType<UnaryExpression>(expression);
        var unaryExpression = (UnaryExpression)expression;
        Assert.Equal(ExpressionType.Not, unaryExpression.NodeType);
        
        var methodCall = Assert.IsAssignableFrom<MethodCallExpression>(unaryExpression.Operand);
        Assert.Equal("Equals", methodCall.Method.Name);
        Assert.Equal(_memberExpression, methodCall.Object);
        
        if (comparison.HasValue)
        {
            // Should include StringComparison parameter for case insensitive
            Assert.True(methodCall.Arguments.Count >= 2);
            var lastArg = methodCall.Arguments[methodCall.Arguments.Count - 1];
            if (lastArg is ConstantExpression constantExpr && constantExpr.Value is StringComparison actualComparison)
            {
                Assert.Equal(comparison.Value, actualComparison);
            }
        }
    }

    [Fact]
    public void AllMethods_ShouldCompileAndExecuteCorrectly()
    {
        // Arrange
        var testData = new[] { new TestObject { Name = "Test Value" } }.AsQueryable();
        var defaultSettings = CreateSettings();
        var caseInsensitiveSettings = CreateSettings(StringComparison.OrdinalIgnoreCase);

        // Test case-sensitive operations (null = default behavior)
        var startsWithExpr = Expression.Lambda<Func<TestObject, bool>>(
            _memberExpression.StartsWith("Test", defaultSettings), _parameter);
        var endsWithExpr = Expression.Lambda<Func<TestObject, bool>>(
            _memberExpression.EndsWith("Value", defaultSettings), _parameter);
        var containsExpr = Expression.Lambda<Func<TestObject, bool>>(
            _memberExpression.Contains("st Val", defaultSettings), _parameter);
        var equalsExpr = Expression.Lambda<Func<TestObject, bool>>(
            _memberExpression.Equals("Test Value", defaultSettings), _parameter);
        var notEqualsExpr = Expression.Lambda<Func<TestObject, bool>>(
            _memberExpression.NotEquals("Wrong Value", defaultSettings), _parameter);

        // Test case-insensitive operations
        var startsWithCIExpr = Expression.Lambda<Func<TestObject, bool>>(
            _memberExpression.StartsWith("test", caseInsensitiveSettings), _parameter);
        var endsWithCIExpr = Expression.Lambda<Func<TestObject, bool>>(
            _memberExpression.EndsWith("VALUE", caseInsensitiveSettings), _parameter);
        var containsCIExpr = Expression.Lambda<Func<TestObject, bool>>(
            _memberExpression.Contains("ST VAL", caseInsensitiveSettings), _parameter);
        var equalsCIExpr = Expression.Lambda<Func<TestObject, bool>>(
            _memberExpression.Equals("TEST VALUE", caseInsensitiveSettings), _parameter);
        var notEqualsCIExpr = Expression.Lambda<Func<TestObject, bool>>(
            _memberExpression.NotEquals("WRONG VALUE", caseInsensitiveSettings), _parameter);

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