using Mpt.Rql.Abstractions;
using Mpt.Rql.Linq.Core.Metadata;
using Mpt.Rql.Linq.Settings;
using Mpt.UnitTests.Common.Factory;
using Mpt.UnitTests.Common.Utility;
using System.Text.Json;
using Xunit;

namespace Mpt.Rql.Linq.UnitTests;

public class TypeDescriptionProviderTests
{
    [Fact]
    public void GetDescription_SampleEntity_AllFilter()
    {
        // Arrange
        var provider = MetadataProviderFactory.Public();

        // Act 
        var props = provider.GetPropertiesByDeclaringType(typeof(SampleEntity));

        // Assert
        Assert.NotEmpty(props);
        Assert.True(props.All(e => e.Actions == RqlActions.Filter));
    }


    [Fact]
    public void GetDescription_SampleEntity_CustomAttributes()
    {

        // Arrange
        var globalSettings = new GlobalRqlSettings();
        globalSettings.General.DefaultActions = RqlActions.Filter | RqlActions.Order;

        IRqlMetadataProvider provider = new MetadataProvider(
            new PropertyNameProvider(),
            new MetadataFactory(globalSettings));

        // Act 
        var props = provider.GetPropertiesByDeclaringType(typeof(SampleEntity));

        // Assert
        Assert.NotEmpty(props);
        Assert.True(props.All(e => e.Actions == (RqlActions.Filter | RqlActions.Order)));
        Assert.Contains(props, e => e.Name == JsonNamingPolicy.CamelCase.ConvertName(nameof(SampleEntity.Types)) && e.Type == RqlPropertyType.Collection);
        Assert.Contains(props, e => e.Name == JsonNamingPolicy.CamelCase.ConvertName(nameof(SampleEntity.ModifiedDate)) && e.Type == RqlPropertyType.Primitive);
        Assert.Contains(props, e => e.Name == JsonNamingPolicy.CamelCase.ConvertName(nameof(SampleEntity.Size)) && e.Type == RqlPropertyType.Primitive);
    }

    [Theory]
    [InlineData(RqlActions.All)]
    [InlineData(RqlActions.Filter)]
    [InlineData(RqlActions.None)]
    [InlineData(RqlActions.Select)]
    [InlineData(RqlActions.Order)]
    public void GetDescription_DescriptionSampleEntity_GlobalPropDoesNotChange(RqlActions globalAction)
    {

        // Arrange
        var globalSettings = new GlobalRqlSettings();
        globalSettings.General.DefaultActions = globalAction;

        IRqlMetadataProvider provider = new MetadataProvider(
            new PropertyNameProvider(),
            new MetadataFactory(globalSettings));

        // Act 
        var props = provider.GetPropertiesByDeclaringType(typeof(SampleTypeDescriptionEntity));

        // Assert
        Assert.NotEmpty(props);
        Assert.Contains(props, e => e.Name == JsonNamingPolicy.CamelCase.ConvertName(nameof(SampleTypeDescriptionEntity.OrderProp)) && e.Actions == RqlActions.Order);
        Assert.Contains(props, e => e.Name == JsonNamingPolicy.CamelCase.ConvertName(nameof(SampleTypeDescriptionEntity.AllProp)) && e.Actions == RqlActions.All);
        Assert.Contains(props, e => e.Name == JsonNamingPolicy.CamelCase.ConvertName(nameof(SampleTypeDescriptionEntity.FilterProp)) && e.Actions == RqlActions.Filter);
        Assert.Contains(props, e => e.Name == JsonNamingPolicy.CamelCase.ConvertName(nameof(SampleTypeDescriptionEntity.SelectProp)) && e.Actions == RqlActions.Select);
        Assert.Contains(props, e => e.Name == JsonNamingPolicy.CamelCase.ConvertName(nameof(SampleTypeDescriptionEntity.NoneProp)) && e.Actions == RqlActions.None);
    }
}