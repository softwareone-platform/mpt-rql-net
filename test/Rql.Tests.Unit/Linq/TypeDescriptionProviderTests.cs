using Rql.Tests.Unit.Factory;
using Rql.Tests.Unit.Utility;
using SoftwareOne.Rql;
using SoftwareOne.Rql.Abstractions;
using SoftwareOne.Rql.Linq.Configuration;
using SoftwareOne.Rql.Linq.Core.Metadata;
using System.Text.Json;
using Xunit;

namespace Rql.Tests.Unit.Linq;

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
        IRqlMetadataProvider provider = new MetadataProvider(
            new PropertyNameProvider(),
            new MetadataFactory(new RqlGeneralSettings { DefaultActions = RqlActions.Filter | RqlActions.Order }));

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
        IRqlMetadataProvider provider = new MetadataProvider(
            new PropertyNameProvider(), 
            new MetadataFactory(new RqlGeneralSettings{ DefaultActions = globalAction}));

        // Act 
        var props = provider.GetPropertiesByDeclaringType(typeof(SampleTypeDescriptionEntity));

        // Assert
        Assert.NotEmpty(props);
        Assert.Contains(props, e => e.Name == JsonNamingPolicy.CamelCase.ConvertName(nameof(SampleTypeDescriptionEntity.OrderProp)) && e.Actions == RqlActions.Order);
        Assert.Contains(props, e => e.Name == JsonNamingPolicy.CamelCase.ConvertName(nameof(SampleTypeDescriptionEntity.AllProp)) && e.Actions == RqlActions.All);
        Assert.Contains(props, e => e.Name == JsonNamingPolicy.CamelCase.ConvertName(nameof(SampleTypeDescriptionEntity.FilterProp)) && e.Actions == RqlActions.Filter);
        Assert.Contains(props, e => e.Name == JsonNamingPolicy.CamelCase.ConvertName(nameof(SampleTypeDescriptionEntity.SelectProp)) && e.Actions == RqlActions.Select);
        Assert.Contains(props, e => e.Name == JsonNamingPolicy.CamelCase.ConvertName(nameof(SampleTypeDescriptionEntity.NoneProp))  && e.Actions == RqlActions.None);
    }
}