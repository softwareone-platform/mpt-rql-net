using SoftwareOne.Rql.Abstractions.Argument;
using SoftwareOne.Rql.Abstractions.Collection;
using SoftwareOne.Rql.Parsers.Linear.Domain.Core;
using SoftwareOne.Rql.Parsers.Linear.Domain.Services;
using SoftwareOne.UnitTests.Common;
using Xunit;

namespace Rql.Tests.Unit.Parsers.Linear.Domain.Services;

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