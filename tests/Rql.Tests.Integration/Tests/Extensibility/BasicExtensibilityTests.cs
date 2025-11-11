using Rql.Tests.Integration.Core;
using Rql.Tests.Integration.Tests.Extensibility.Utility;
using Mpt.Rql;
using Xunit;
using Mpt.Rql.Services.Filtering.Operators.Comparison;
using Mpt.Rql.Services.Filtering.Operators.List;
using Mpt.Rql.Services.Filtering.Operators.Search;

namespace Rql.Tests.Integration.Tests.Extensibility;

public class BasicExtensibilityTests
{
    [Theory]
    [InlineData(typeof(Product), typeof(ProductView), true)]
    [InlineData(typeof(Product), typeof(Product), false)]
    public void Mapper_ProductViewMap_CheckRegistered(Type storage, Type view, bool registered)
    {
        // Arrange
        var mapperType = typeof(IRqlMapper<,>).MakeGenericType(storage, view);

        // Act
        var provider = RqlFactory.MakeProvider(services => { }, config => config.ScanForMappers(typeof(ProductView).Assembly));
        var service = provider.GetService(mapperType);

        // Assert
        if (registered)
        {
            Assert.NotNull(service);
            Assert.IsType<ProductViewMap>(service);
        }
        else
        {
            Assert.Null(service);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Operator_CustomEqualHandler_CheckRegistered(bool registered)
    {
        // Act
        var provider = RqlFactory.MakeProvider(services => { }, config =>
        {
            if (registered)
                config.SetComparisonHandler<IEqual, CustomEqualHandler>();
        });
        var service = provider.GetService(typeof(IEqual));

        // Assert
        Assert.NotNull(service);
        if (registered)
            Assert.IsType<CustomEqualHandler>(service);
        else
            Assert.IsNotType<CustomEqualHandler>(service);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Operator_CustomSearchHandler_CheckRegistered(bool registered)
    {
        // Act
        var provider = RqlFactory.MakeProvider(services => { }, config =>
        {
            if (registered)
                config.SetSearchHandler<ILike, CustomLikeHandler>();
        });
        var service = provider.GetService(typeof(ILike));

        // Assert
        Assert.NotNull(service);
        if (registered)
            Assert.IsType<CustomLikeHandler>(service);
        else
            Assert.IsNotType<CustomLikeHandler>(service);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Operator_CustomListHandler_CheckRegistered(bool registered)
    {
        // Act
        var provider = RqlFactory.MakeProvider(services => { }, config =>
        {
            if (registered)
                config.SetListHandler<IListIn, CustomListHandler>();
        });
        var service = provider.GetService(typeof(IListIn));

        // Assert
        Assert.NotNull(service);
        if (registered)
            Assert.IsType<CustomListHandler>(service);
        else
            Assert.IsNotType<CustomListHandler>(service);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Provider_CustomPropertyName_CheckRegistered(bool registered)
    {
        // Act
        var provider = RqlFactory.MakeProvider(services => { }, config =>
        {
            if (registered)
                config.SetPropertyNameProvider<CustomPropertyNameProvider>();
        });
        var service = provider.GetService(typeof(IPropertyNameProvider));

        // Assert
        Assert.NotNull(service);
        if (registered)
            Assert.IsType<CustomPropertyNameProvider>(service);
        else
            Assert.IsNotType<CustomPropertyNameProvider>(service);
    }
}