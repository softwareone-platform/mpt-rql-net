using FluentAssertions;
using Mpt.Rql;
using Mpt.Rql.Core.Metadata;
using Mpt.Rql.Services.Mapping;
using Xunit;

namespace Rql.Tests.Unit.Services.Mapping;

public class RqlMapperContextTests
{
    [Fact]
    public void AddMissing_AutoMaps_UnmappedMatchingProperties_AsDynamic()
    {
        // Arrange
        var ctx = MakeContext<Storage, View>();
        // Pre-map one property to ensure it's not auto-mapped again
        ctx.MapStatic(v => v.Renamed, s => s.Different);

        // Act
        ctx.AddMissing();

        // Assert
        var mapping = ctx.Mapping;
        // Name and Count should be auto-mapped
        mapping.Should().ContainKey(nameof(View.Name));
        mapping[nameof(View.Name)].IsDynamic.Should().BeTrue();
        mapping[nameof(View.Name)].TargetProperty.Property.Name.Should().Be(nameof(View.Name));

        mapping.Should().ContainKey(nameof(View.Count));
        mapping[nameof(View.Count)].IsDynamic.Should().BeTrue();
        mapping[nameof(View.Count)].TargetProperty.Property.Name.Should().Be(nameof(View.Count));

        // Pre-mapped entry should exist and remain
        mapping.Should().ContainKey(nameof(View.Renamed));
        mapping[nameof(View.Renamed)].IsDynamic.Should().BeFalse();
        mapping[nameof(View.Renamed)].SourceExpression.Should().NotBeNull();

        // Ignored should not be auto-mapped
        mapping.Should().NotContainKey(nameof(View.IgnoredCategory));
    }

    [Fact]
    public void AddMissing_DoesNotMap_IgnoredModeProperties()
    {
        // Arrange
        var ctx = MakeContext<Storage, View>();

        // Act
        ctx.AddMissing();

        // Assert
        ctx.Mapping.ContainsKey(nameof(View.IgnoredCategory)).Should().BeFalse();
    }

    [Fact]
    public void AddMissing_SwitchWithoutDefault_Throws()
    {
        // Arrange
        var ctx = MakeContext<SwitchStorage, SwitchView>();
        // Define a switch without a default case
        ctx.Switch(v => v.Name);

        // Act & Assert
        // Missing default mapping
        Assert.Throws<RqlMappingException>(() => ctx.AddMissing());
    }

    [Fact]
    public void AddMissing_SwitchWithDefault_IsAdded()
    {
        // Arrange
        var ctx = MakeContext<SwitchStorage, SwitchView>();
        ctx.Switch(v => v.Name)
            .Case(s => s.Count > 0, s => s.Name)
            .Default(s => s.Name);

        // Act
        ctx.AddMissing();

        // Assert
        ctx.Mapping.Should().ContainKey(nameof(SwitchView.Name));
        ctx.Mapping[nameof(SwitchView.Name)].IsDynamic.Should().BeTrue();
        ctx.Mapping[nameof(SwitchView.Name)].SourceExpression.Should().NotBeNull();
    }

    private class SwitchStorage
    {
        public string Name { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    private class SwitchView
    {
        public string Name { get; set; } = string.Empty;
        public int Count { get; set; }
    }
    private static RqlMapperContext<TStorage, TView> MakeContext<TStorage, TView>()
    {
        var metadataProvider = new MetadataProvider(new PropertyNameProvider(), new MetadataFactory(new Mpt.Rql.Settings.GlobalRqlSettings()));
        return new RqlMapperContext<TStorage, TView>(metadataProvider);
    }

    private class Storage
    {
        public string Name { get; set; } = string.Empty;
        public int Count { get; set; }
        public string IgnoredCategory { get; set; } = string.Empty;
        public string Different { get; set; } = string.Empty;
    }

    private class View
    {
        public string Name { get; set; } = string.Empty;
        public int Count { get; set; }

        [RqlProperty(Mode = RqlPropertyMode.Ignored)]
        public string IgnoredCategory { get; set; } = string.Empty;

        public string Renamed { get; set; } = string.Empty;
    }
}
