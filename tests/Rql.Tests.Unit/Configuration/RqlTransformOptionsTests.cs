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
    
    [Fact]
    public void ScopeCollection_CovariantElementType_ThrowsBeforeRegistration()
    {
        var act = () => _options.ScopeCollection<ScopeOwner, BaseElement>(
            owner => owner.Elements,
            element => element.IsVisible);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("collection");

        _options.CollectionScopes.Should().BeEmpty();
    }

    [Fact]
    public void ScopeCollection_ExactElementType_RegistersScope()
    {
        _options.ScopeCollection<ScopeOwner, DerivedElement>(
            owner => owner.Elements,
            element => element.IsVisible);

        _options.CollectionScopes.Should().ContainSingle();
    }

    [Fact]
    public void ValidateCollectionScopeSource_DifferentStorageType_Throws()
    {
        _options.ScopeCollection<ScopeOwner, DerivedElement>(
            owner => owner.Elements,
            element => element.IsVisible);

        var act = () =>
            _options.ValidateCollectionScopeSource(typeof(OtherScopeOwner));

        act.Should().Throw<ArgumentException>()
            .WithParameterName("configure");
    }

    [Fact]
    public void ValidateCollectionScopeSource_MatchingStorageType_DoesNotThrow()
    {
        _options.ScopeCollection<ScopeOwner, DerivedElement>(
            owner => owner.Elements,
            element => element.IsVisible);

        var act = () =>
            _options.ValidateCollectionScopeSource(typeof(ScopeOwner));

        act.Should().NotThrow();
    }

    public class BaseElement
    {
        public bool IsVisible { get; set; }
    }

    public sealed class DerivedElement : BaseElement
    {
    }

    public sealed class ScopeOwner
    {
        public ICollection<DerivedElement> Elements { get; set; } = [];
    }

    public sealed class OtherScopeOwner
    {
    }
}
