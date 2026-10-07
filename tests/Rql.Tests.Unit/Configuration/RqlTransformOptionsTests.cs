using FluentAssertions;
using Mpt.Rql;
using Mpt.Rql.Settings;
using Xunit;

namespace Rql.Tests.Unit.Configuration;

public class RqlTransformOptionsTests
{
    private readonly RqlTransformOptions _options = new(new GlobalRqlSettings());

    [Fact]
    public void Decisions_WithoutDecisions_IsNull()
    {
        // Act
        var decisions = _options.Decisions;

        // Assert
        decisions.Should().BeNull();
    }

    [Fact]
    public void Override_PathAlreadyDecidedOnInAnotherCase_ReplacesTheDecision()
    {
        // Arrange
        _options.Override("Reference.Name", include: false);

        // Act
        _options.Override("reference.NAME", include: true);

        // Assert
        _options.Decisions.Should().ContainSingle().Which.Value.Should().BeTrue();
        _options.Decisions!["REFERENCE.name"].Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Override_WithoutPath_Throws(string? path)
    {
        // Act
        var act = () => _options.Override(path!, include: true);

        // Assert
        act.Should().Throw<ArgumentException>();
    }
}
