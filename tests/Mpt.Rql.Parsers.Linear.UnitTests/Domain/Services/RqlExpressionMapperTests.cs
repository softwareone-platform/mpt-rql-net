using Mpt.Rql.Abstractions.Argument;
using Mpt.Rql.Parsers.Linear.Domain.Core.ValueTypes;
using Mpt.Rql.Parsers.Linear.Domain.Services;
using Xunit;

namespace Rql.Tests.Unit.Parsers.Linear.Domain.Services;

public class RqlExpressionMapperTests
{
    [Fact]
    public void MapFromWord_WithValidOrStringWithBrackets_ReturnsRqlConstantForFirstWord()
    {
        // Arrange
        const int statusWordStart = 3;
        const int statusWordLength = 6;
        var testString = "eq(status,processing)";
        var word = Word.Make(testString.AsMemory(), 0);
        word.WordStart = statusWordStart;
        word.WordLength = statusWordLength;

        // Act
        var actualResult = RqlExpressionMapper.MapFromWord(word);

        // Assert
        Assert.IsType<RqlConstant>(actualResult);
    }
}