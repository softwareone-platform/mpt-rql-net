using Mpt.Rql.Abstractions.Configuration;
using Mpt.Rql.Settings;
using Xunit;

namespace Rql.Tests.Integration.Tests.Functionality;

public class SafeNavigationSettingsTests
{
    [Fact]
    public void SafeNavigationSettings_DefaultValues_AreOff()
    {
        // Arrange & Act
        var settings = new RqlSettings();
        
        // Assert
        Assert.Equal(SafeNavigationMode.Off, settings.Filter.SafeNavigation);
        Assert.Equal(SafeNavigationMode.Off, settings.Ordering.SafeNavigation);
    }

    [Fact]
    public void SafeNavigationSettings_CanBeSetIndependently()
    {
        // Arrange
        var settings = new RqlSettings();
        
        // Act
        settings.Filter.SafeNavigation = SafeNavigationMode.On;
        settings.Ordering.SafeNavigation = SafeNavigationMode.Off;
        
        // Assert
        Assert.Equal(SafeNavigationMode.On, settings.Filter.SafeNavigation);
        Assert.Equal(SafeNavigationMode.Off, settings.Ordering.SafeNavigation);
    }
}