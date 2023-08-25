using SoftwareOne.Rql.Abstractions.Binary;
using SoftwareOne.Rql.Abstractions.Constant;
using SoftwareOne.Rql.Abstractions.Exception;
using SoftwareOne.Rql.Parsers.Linear.Domain.Core.ValueTypes;
using SoftwareOne.Rql.Parsers.Linear.Domain.Services;
using Xunit;

namespace Rql.Tests.Unit.Parsers.Linear;

public class RqlExpressionMapperTests
{
    [Fact]
    public void MapFromWord_WithValidEqualString_ReturnsRqlEqual()
    {
        // Arrange
        const int delimiterIndex = 6;
        var testString = "field1=value1";
        var word = Word.Make(testString.AsMemory(), 0);
        word.Delimiters.Add(delimiterIndex);
        word.WordLength = testString.Length;

        // Act
        var actualResult = RqlExpressionMapper.MapFromWord(word); 

        // Assert
        var or = Assert.IsType<RqlEqual>(actualResult);
        Assert.IsType<RqlConstant>(or.Left);
        Assert.IsType<RqlConstant>(or.Right);
    }

    [Fact]
    public void MapFromWord_WithValidOrStringWithBrackets_ReturnsRqlConstantForFirstWord()
    {
        // Arrange
        const int statusWordStart = 3;
        const int statusWordLength = 6;
        var testString = "eq(status,processing)";
        var word = Word.Make(testString.AsMemory(), 0);
        word.WordStart= statusWordStart;
        word.WordLength = statusWordLength;

        // Act
        var actualResult = RqlExpressionMapper.MapFromWord(word);

        // Assert
        Assert.IsType<RqlConstant>(actualResult);
    }

    [Fact]
    public void MapFromWord_WithTooManyDelimiters_ThrowsRqlExpressionMapperException()
    {
        // Arrange
        const int firstDelimiterIndex = 6;
        const int secondDelimiterIndex = 9;
        var testString = "status=eq=processing";
        var word = Word.Make(testString.AsMemory(), 0);
        word.Delimiters.Add(firstDelimiterIndex);
        word.Delimiters.Add(secondDelimiterIndex);
        word.WordLength = testString.Length;

        // Act and Assert
        Assert.Throws<RqlExpressionMapperException>(() => RqlExpressionMapper.MapFromWord(word));
    }
}