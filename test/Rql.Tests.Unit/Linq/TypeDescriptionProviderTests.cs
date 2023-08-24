using Rql.Tests.Unit.Factory;
using Rql.Tests.Unit.Utility;
using SoftwareOne.Rql;
using SoftwareOne.Rql.Abstractions;
using SoftwareOne.Rql.Linq.Configuration;
using SoftwareOne.Rql.Linq.Core.Metadata;
using Xunit;

namespace Rql.Tests.Unit.Linq;

public class TypeDescriptionProviderTests
{
    [Fact]
    public void GetDescription_SampleEntity_AllFilter()
    {
        // Arrange
        var provider = TypeMetadataProviderFactory.Public();

        // Act 
        var props = provider.GetPropertiesByDeclaringType(typeof(SampleEntity));

        // Assert
        Assert.NotEmpty(props);
        Assert.True(props.All(e => e.Actions == RqlAction.Filter));
    }


    [Fact]
    public void GetDescription_SampleEntity_CustomAttributes()
    {

        // Arrange
        IRqlMetadataProvider provider = new MetadataProvider(
            new PropertyNameProvider(),
            new MetadataFactory(new RqlSettings { DefaultActions = RqlAction.Filter | RqlAction.Order }));

        // Act 
        var props = provider.GetPropertiesByDeclaringType(typeof(SampleEntity));

        // Assert
        Assert.NotEmpty(props);
        Assert.True(props.All(e => e.Actions == (RqlAction.Filter | RqlAction.Order)));
        Assert.Contains(props, e => e.Name == nameof(SampleEntity.Types) && e.Type == RqlPropertyType.Collection);
        Assert.Contains(props, e => e.Name == nameof(SampleEntity.ModifiedDate) && e.Type == RqlPropertyType.Primitive);
        Assert.Contains(props, e => e.Name == nameof(SampleEntity.Size) && e.Type == RqlPropertyType.Primitive);
    }

    [Theory]
    [InlineData(RqlAction.All)]
    [InlineData(RqlAction.Filter)]
    [InlineData(RqlAction.None)]
    [InlineData(RqlAction.Select)]
    [InlineData(RqlAction.Order)]
    public void GetDescription_DescriptionSampleEntity_GlobalPropDoesNotChange(RqlAction globalAction)
    {

        // Arrange
        IRqlMetadataProvider provider = new MetadataProvider(
            new PropertyNameProvider(), 
            new MetadataFactory(new RqlSettings{ DefaultActions = globalAction}));

        // Act 
        var props = provider.GetPropertiesByDeclaringType(typeof(SampleTypeDescriptionEntity));

        // Assert
        Assert.NotEmpty(props);
        Assert.Contains(props, e => e.Name == nameof(SampleTypeDescriptionEntity.OrderProp) && e.Actions == RqlAction.Order);
        Assert.Contains(props, e => e.Name == nameof(SampleTypeDescriptionEntity.AllProp) && e.Actions == RqlAction.All);
        Assert.Contains(props, e => e.Name == nameof(SampleTypeDescriptionEntity.FilterProp) && e.Actions == RqlAction.Filter);
        Assert.Contains(props, e => e.Name == nameof(SampleTypeDescriptionEntity.SelectProp) && e.Actions == RqlAction.Select);
        Assert.Contains(props, e => e.Name == nameof(SampleTypeDescriptionEntity.NoneProp)  && e.Actions == RqlAction.None);
    }
}