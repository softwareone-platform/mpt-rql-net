using Mpt.Rql.Abstractions.Result;
using Xunit;

namespace Rql.Tests.Unit.Result;

public class ErrorTests
{
    [Fact]
    public void Validation_WithMessage_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var message = "Validation error occurred";

        // Act
        var error = Error.Validation(message);

        // Assert
        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.Equal("rql_validation", error.Code);
        Assert.Equal(message, error.Message);
    }

    [Fact]
    public void Validation_WithMessageAndCode_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var message = "Validation error occurred";
        var code = "custom_code";

        // Act
        var error = Error.Validation(message, code);

        // Assert
        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.Equal(code, error.Code);
        Assert.Equal(message, error.Message);
    }

    [Fact]
    public void Validation_WithMessageCodeAndPath_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var message = "Validation error occurred";
        var code = "custom_code";
        var path = "custom_path";

        // Act
        var error = Error.Validation(message, code, path);

        // Assert
        Assert.Equal(ErrorType.Validation, error.Type);
        Assert.Equal(code, error.Code);
        Assert.Equal(message, error.Message);
        Assert.Equal(path, error.Path);
    }

    [Fact]
    public void General_WithMessage_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var message = "General error occurred";

        // Act
        var error = Error.General(message);

        // Assert
        Assert.Equal(ErrorType.General, error.Type);
        Assert.Equal("rql_failure", error.Code);
        Assert.Equal(message, error.Message);
    }

    [Fact]
    public void General_WithMessageAndCode_ShouldSetPropertiesCorrectly()
    {
        // Arrange
        var message = "General error occurred";
        var code = "custom_code";

        // Act
        var error = Error.General(message, code);

        // Assert
        Assert.Equal(ErrorType.General, error.Type);
        Assert.Equal(code, error.Code);
        Assert.Equal(message, error.Message);
    }

    [Fact]
    public void ToString_ShouldReturnCorrectFormat()
    {
        // Arrange
        var message = "General error occurred";
        var code = "custom_code";
        var error = Error.General(message, code);

        // Act
        var result = error.ToString();

        // Assert
        Assert.Equal("General: custom_code - General error occurred", result);
    }
}