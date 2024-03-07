using Rql.Tests.Unit.Factory;
using Rql.Tests.Unit.Utility;
using SoftwareOne.Rql;
using SoftwareOne.Rql.Abstractions;
using SoftwareOne.Rql.Linq.Configuration;
using SoftwareOne.Rql.Linq.Core.Metadata;
using System.Text.Json;
using Xunit;

namespace Rql.Tests.Unit.Linq;

public class MetadataProviderTest
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
    public void GetMetadata_ByAction_GlobalPropDoesNotChange(RqlActions globalAction)
    {
        // Arrange
        IRqlMetadataProvider provider = new MetadataProvider(
            new PropertyNameProvider(),
            new MetadataFactory(new RqlGeneralSettings { DefaultActions = globalAction }));

        // Act 
        var props = provider.GetPropertiesByDeclaringType(typeof(MetadataActionTestEntity));

        // Assert
        Assert.NotEmpty(props);
        Assert.Contains(props, e => e.Name == JsonNamingPolicy.CamelCase.ConvertName(nameof(MetadataActionTestEntity.OrderProp)) && e.Actions == RqlActions.Order);
        Assert.Contains(props, e => e.Name == JsonNamingPolicy.CamelCase.ConvertName(nameof(MetadataActionTestEntity.AllProp)) && e.Actions == RqlActions.All);
        Assert.Contains(props, e => e.Name == JsonNamingPolicy.CamelCase.ConvertName(nameof(MetadataActionTestEntity.FilterProp)) && e.Actions == RqlActions.Filter);
        Assert.Contains(props, e => e.Name == JsonNamingPolicy.CamelCase.ConvertName(nameof(MetadataActionTestEntity.SelectProp)) && e.Actions == RqlActions.Select);
        Assert.Contains(props, e => e.Name == JsonNamingPolicy.CamelCase.ConvertName(nameof(MetadataActionTestEntity.NoneProp)) && e.Actions == RqlActions.None);
    }


    [Theory]
    [InlineData(nameof(MetadataOperatorTestEntity.StringDefaults), RqlOperators.StringDefaults)]
    [InlineData(nameof(MetadataOperatorTestEntity.GenericDefaults), RqlOperators.GenericDefaults)]
    [InlineData(nameof(MetadataOperatorTestEntity.GenericNullableDefaults), RqlOperators.GenericDefaults | RqlOperators.Null)]
    [InlineData(nameof(MetadataOperatorTestEntity.GenericDefaults), RqlOperators.GenericDefaults ^ RqlOperators.Ge, false)]
    [InlineData(nameof(MetadataOperatorTestEntity.GuidDefaults), RqlOperators.GuidDefaults)]
    [InlineData(nameof(MetadataOperatorTestEntity.NullableGuidDefaults), RqlOperators.GuidDefaults | RqlOperators.Null)]
    [InlineData(nameof(MetadataOperatorTestEntity.GreaterThanLessThan), RqlOperators.Gt | RqlOperators.Lt)]
    [InlineData(nameof(MetadataOperatorTestEntity.None), RqlOperators.None)]
    public void GetMetadata_Operator_MustMatch(string propertyName, RqlOperators rqlOperator, bool isHappyFlow = true)
    {
        // Arrange
        IRqlMetadataProvider provider = new MetadataProvider(
            new PropertyNameProvider(),
            new MetadataFactory(new RqlGeneralSettings()));

        // Act 
        var props = provider.GetPropertiesByDeclaringType(typeof(MetadataOperatorTestEntity));

        // Assert
        Assert.NotEmpty(props);

        var propertyInfo = props.FirstOrDefault(e => e.Property!.Name == propertyName);
        Assert.NotNull(propertyInfo);
        var toCompare = rqlOperator ^ propertyInfo.Operators;

        if (isHappyFlow)
            Assert.Equal(RqlOperators.None, toCompare);
        else
            Assert.NotEqual(RqlOperators.None, toCompare);
    }
}