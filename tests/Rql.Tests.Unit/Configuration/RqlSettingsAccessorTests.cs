using Mpt.Rql;
using Mpt.Rql.Abstractions.Configuration.Filter;
using Mpt.Rql.Settings;
using System.Text.Json;
using Xunit;

namespace Rql.Tests.Unit.Configuration;

public class RqlSettingsAccessorTests
{
    [Fact]
    public void GetInstance_ShouldDeepCloneAllPropertiesFromGlobalSettings()
    {
        // Arrange
        var globalSettings = new GlobalRqlSettings();

        // Set some test values different from defaults
        globalSettings.General.DefaultActions = RqlActions.Filter | RqlActions.Order;
        globalSettings.Mapping.Transparent = true;
        globalSettings.Select.Implicit = RqlSelectModes.All;
        globalSettings.Select.Explicit = RqlSelectModes.Core;
        globalSettings.Select.MaxDepth = 42;
        globalSettings.Filter.Strings.ComparisonType = StringComparisonType.Lexicographical;

        var accessor = new RqlSettingsAccessor(globalSettings);

        // Act
        var instance1 = accessor.GetInstance();
        var instance2 = accessor.GetInstance();

        // Assert - Same instance returned (cached)
        Assert.Same(instance1, instance2);

        // Assert - Deep clone verification using JSON comparison
        var globalJson = JsonSerializer.Serialize((RqlSettings)globalSettings);
        var instanceJson = JsonSerializer.Serialize(instance1);

        // Values should be identical
        Assert.Equal(globalJson, instanceJson);

        // Objects should be different instances (deep clone)
        Assert.NotSame(globalSettings, instance1);

        // Modify instance to ensure it doesn't affect global
        instance1.Mapping.Transparent = !globalSettings.Mapping.Transparent;
        Assert.NotEqual(globalSettings.Mapping.Transparent, instance1.Mapping.Transparent);
    }
}