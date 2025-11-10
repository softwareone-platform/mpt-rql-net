using Mpt.Rql.Abstractions.Argument;
using Mpt.Rql.Abstractions.Collection;
using Mpt.Rql.Parsers.Linear.Domain.Core;
using Mpt.Rql.Parsers.Linear.Domain.Services;
using Mpt.UnitTests.Common.Factory;
using Xunit;

namespace Mpt.Rql.Parsers.Linear.UnitTests.Domain.Services;

public class RqlCollectionParserTests
{
    public RqlCollectionParserTests()
    {
    }

    [Fact]
    public void Parse_WhenSuccessfulAnyInput_ResolvesToRqlAny()
    {
        // Act
        var actualResult = RqlCollectionParser.Parse(Constants.RqlTerm.Any, RqlExpressionFactory.Default());

        // Assert
        Assert.Equal(typeof(RqlAny), actualResult.GetType());
        Assert.Equal(typeof(RqlConstant), ((RqlAny)actualResult).Right!.GetType());
    }

    [Fact]
    public void Parse_WhenSuccessfulAllInput_ResolvesToRqlAll()
    {
        // Act
        var actualResult = RqlCollectionParser.Parse(Constants.RqlTerm.All, RqlExpressionFactory.Default());

        // Assert
        Assert.Equal(typeof(RqlAll), actualResult.GetType());
        Assert.Equal(typeof(RqlConstant), ((RqlAll)actualResult).Right!.GetType());
    }
}