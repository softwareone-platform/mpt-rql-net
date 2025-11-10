using Mpt.Rql.Abstractions.Result;
using Mpt.Rql.Linq.Core.Result;
using Xunit;

namespace Mpt.Rql.Linq.UnitTests.Result;

public class ResultTests
{
    [Fact]
    public void Result_WithValue_ShouldContainValue()
    {
        // Arrange
        const string value = "Test Value";
        var result = new Result<string>(value);

        // Act & Assert
        Assert.False(result.IsError);
        Assert.Equal(value, result.Value);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Result_WithSingleError_ShouldContainError()
    {
        // Arrange
        var error = Error.General("Error occurred");
        var result = new Result<string>(null, new List<Error> { error });

        // Act & Assert
        Assert.True(result.IsError);
        Assert.Null(result.Value);
        Assert.Single(result.Errors);
        Assert.Equal(error, result.Errors[0]);
    }

    [Fact]
    public void Result_WithListOfErrors_ShouldContainErrors()
    {
        // Arrange
        var errors = new List<Error>
        {
            Error.General("Error1"),
            Error.Validation("Error2")
        };
        var result = new Result<string>(null, errors);

        // Act & Assert
        Assert.True(result.IsError);
        Assert.Null(result.Value);
        Assert.Equal(2, result.Errors.Count);
        Assert.Equal(errors[0], result.Errors[0]);
        Assert.Equal(errors[1], result.Errors[1]);
    }

    [Fact]
    public void ImplicitConversion_FromValue_ShouldCreateResult()
    {
        // Arrange
        const string value = "Test Value";
        Result<string> result = value;

        // Act & Assert
        Assert.False(result.IsError);
        Assert.Equal(value, result.Value);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ImplicitConversion_FromError_ShouldCreateResult()
    {
        // Arrange
        var error = Error.General("Error occurred");
        Result<string> result = error;

        // Act & Assert
        Assert.True(result.IsError);
        Assert.Null(result.Value);
        Assert.Single(result.Errors);
        Assert.Equal(error, result.Errors[0]);
    }

    [Fact]
    public void ImplicitConversion_FromListOfErrors_ShouldCreateResult()
    {
        // Arrange
        var errors = new List<Error>
        {
            Error.General("Error1"),
            Error.Validation("Error2")
        };
        Result<string> result = errors;

        // Act & Assert
        Assert.True(result.IsError);
        Assert.Null(result.Value);
        Assert.Equal(2, result.Errors.Count);
        Assert.Equal(errors[0], result.Errors[0]);
        Assert.Equal(errors[1], result.Errors[1]);
    }
}