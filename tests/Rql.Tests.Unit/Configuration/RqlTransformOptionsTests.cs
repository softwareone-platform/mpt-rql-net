using FluentAssertions;
using Mpt.Rql;
using Mpt.Rql.Settings;
using Xunit;

namespace Rql.Tests.Unit.Configuration;

public class RqlTransformOptionsTests
{
    private readonly RqlTransformOptions _options = new(new GlobalRqlSettings());

    [Fact]
    public void Decisions_WithoutVisibility_IsNull()
    {
        // Act
        var decisions = _options.Decisions;

        // Assert
        decisions.Should().BeNull();
    }

    [Fact]
    public void SetVisibility_PathAlreadySetInAnotherCase_ReplacesTheVisibility()
    {
        // Arrange
        _options.SetVisibility("Reference.Name", RqlVisibility.Hidden);

        // Act
        _options.SetVisibility("reference.NAME", RqlVisibility.Shown);

        // Assert
        _options.Decisions.Should().ContainSingle().Which.Value.Should().Be(RqlVisibility.Shown);
        _options.Decisions!["REFERENCE.name"].Should().Be(RqlVisibility.Shown);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void SetVisibility_WithoutPath_Throws(string? path)
    {
        // Act
        var act = () => _options.SetVisibility(path!, RqlVisibility.Shown);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void SetVisibility_UndefinedVisibility_Throws()
    {
        // Act
        var act = () => _options.SetVisibility("name", (RqlVisibility)42);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
