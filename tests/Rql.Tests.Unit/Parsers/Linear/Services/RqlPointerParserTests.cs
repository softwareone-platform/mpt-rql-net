using Mpt.Rql.Abstractions.Argument;
using Mpt.Rql.Abstractions.Argument.Pointer;
using Mpt.Rql.Abstractions.Exception;
using Mpt.Rql.Parsers.Linear.Domain.Core;
using Mpt.Rql.Parsers.Linear.Domain.Services;
using Mpt.UnitTests.Common.Factory;
using Xunit;

namespace Mpt.Rql.Parsers.Linear.UnitTests.Domain.Services;

public class RqlPointerParserTests
{
    public RqlPointerParserTests()
    {
    }

    [Fact]
    public void Parse_WhenSuccessfulEmptySelfInput_ResolvesToRqlSelf()
    {
        // Act
        var actualResult = RqlPointerParser.Parse(Constants.RqlTerm.Self, RqlExpressionFactory.EmptyList());

        // Assert
        var self = Assert.IsType<RqlSelf>(actualResult);
        Assert.Null(self.Inner);
    }

    [Fact]
    public void Parse_WhenSuccessfulNotEmptySelfInput_ResolvesToRqlSelf()
    {
        // Act
        var actualResult = RqlPointerParser.Parse(Constants.RqlTerm.Self, RqlExpressionFactory.ConstantList(1));

        // Assert
        var self = Assert.IsType<RqlSelf>(actualResult);
        Assert.NotNull(self.Inner);
        Assert.IsType<RqlConstant>(self.Inner);
    }

    [Fact]
    public void Parse_WhenInvalidNotEmptySelfInput_ThrowsRqlPointerParserException()
    {
        // Act and Assert
        Assert.Throws<RqlPointerParserException>(() => RqlPointerParser.Parse(Constants.RqlTerm.Self, RqlExpressionFactory.ConstantList(2)));
    }
}