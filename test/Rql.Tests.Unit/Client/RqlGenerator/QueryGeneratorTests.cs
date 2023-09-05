using FluentAssertions;
using Rql.Tests.Unit.Client.Models;
using Rql.Tests.Unit.Factory;
using SoftwareOne.Rql.Client;
using Xunit;

namespace Rql.Tests.Unit.Client.RqlGenerator;

public class QueryGeneratorTests
{
    private readonly IRqlRequestBuilderProvider _builderProvider;

    public QueryGeneratorTests()
    {
        _builderProvider = new TestRqlRequestBuilderProvider();
    }

    [Fact]
    public void WhenEverythingIsEmpty_ThenEmptyQueryIsGenerated()
    {
        // Arrange & Act
        var rql = _builderProvider.GetRqlRequestBuilder<User>().Build();

        // Assert
        rql.Select.Should().Be(default);
        rql.Order.Should().Be(default);
        rql.Filter.Should().Be(default);
    }

    [Fact]
    public void WhenQueryIsEmptyButHasSelect_ThenOnlySelectIsGenerated()
    {
        // Arrange & Act
        var rql = _builderProvider.GetRqlRequestBuilder<User>()
            .Select(e => e.Include(e => e.FirstName))
            .Build();

        // Assert
        rql.Select.Should().Be("FirstName");
        rql.Order.Should().Be(default);
        rql.Filter.Should().Be(default);
    }

    [Fact]
    public void WhenSelectIsEmptyButHasSelect_ThenOnlyOrderIsGenerated()
    {
        // Arrange & Act
        var rql = _builderProvider.GetRqlRequestBuilder<User>()
            .Order(e => e.OrderBy(e => e.FirstName))
            .Build();

        // Assert
        rql.Order.Should().Be("FirstName");
        rql.Select.Should().Be(default);
        rql.Filter.Should().Be(default);
    }
    
    [Fact]
    public void WhenQueryIsNotEmptyAndSelectAndPagingAreNotDefined_ThenQueryIsGeneratedInRql()
    {
        // Arrange & Act
        var rql = _builderProvider.GetRqlRequestBuilder<User>()
            .Where(context => context.Eq(x => x.HomeAddress.Street, "abc"))
            .Build();

        // Assert
        rql.Filter.Should().Be("eq(HomeAddress.Street, 'abc')");
        rql.Select.Should().Be(default);
        rql.Order.Should().Be(default);
    }

    [Fact]
    public void WhenQueryAndSelect_ThenQueryIsGeneratedInRql()
    {
        // Arrange & Act
        var query = _builderProvider.GetRqlRequestBuilder<User>()
            .Where(context => context.Eq(x => x.HomeAddress.Street, "abc"))
            .Select(c=>c.Exclude(x=>x.HomeAddress))
            .Build();

        // Assert
        query.Filter.Should().Be("eq(HomeAddress.Street, 'abc')");
        query.Select.Should().Be("-HomeAddress");
    }

    [Fact]
    public void WhenQueryAndSelectAreNotEmptyAndShortSyntax_ThenAreGeneratedInRql()
    {
        // Arrange & Act
        var rql = _builderProvider.GetRqlRequestBuilder<User>()
                .Where( context => context.Eq(x => x.HomeAddress.Street, "abc"))
                .Select(context => context.Include(x => x.HomeAddress).Exclude(x => x.OfficeAddress))
                .Build();

        // Assert
        rql.Filter.Should().Be("eq(HomeAddress.Street, 'abc')");
        rql.Select.Should().Be("HomeAddress,-OfficeAddress");
    }

  
    [Fact]
    public void WhenQueryAndSelectAreNotEmptyIsNotDefined_ThenAreGeneratedInRql()
    {
        // Arrange & Act
        var query = _builderProvider.GetRqlRequestBuilder<User>()
            .Where(context => context.Eq(x => x.HomeAddress.Street, "abc"))
            .Order(context => context.OrderByDescending(x => x.FirstName).ThenBy(x => x.LastName))
            .Select(context => context.Include(x => x.HomeAddress).Exclude(x => x.OfficeAddress))
            .Build();
        
        // Assert
        query.Filter.Should().Be("eq(HomeAddress.Street, 'abc')");
        query.Select.Should().Be("HomeAddress,-OfficeAddress");
        query.Order.Should().Be("-FirstName,LastName");
    }

    [Fact]
    public void WhenQuerySelectOrderAndPaging_ThenAreGeneratedInRql()
    {
        // Arrange & Act
        var query = _builderProvider.GetRqlRequestBuilder<User>()
            .Where(context => context.Eq(x => x.HomeAddress.Street, "abc"))
            .Order(context => context.OrderByDescending(x => x.FirstName).ThenBy(x => x.LastName))
            .Select(context => context.Include(x => x.HomeAddress).Exclude(x => x.OfficeAddress))
            .Build();

        // Assert
        query.Filter.Should().Be("eq(HomeAddress.Street, 'abc')");
        query.Select.Should().Be("HomeAddress,-OfficeAddress");
        query.Order.Should().Be("-FirstName,LastName");
    }

    [Fact]
    public void WhenQueryingWithJsonAttributes_ThenJsonNameIsReturnedIfExistsInRql()
    {
        // Arrange & Act
        var query = _builderProvider.GetRqlRequestBuilder<ExampleWithJson>()
            .Where(context => context.Eq(x => x.PropWithoutAttribute, "abc"))
            .Order(context => context.OrderByDescending(x => x.PropWithAttribute).ThenBy(x => x.PropWithoutAttribute))
            .Select(context => context.Include(x => x.PropWithAttribute)
                .Include(x => x.AddressWithAttribute.CityWithoutProp)
                .Include(x => x.AddressWithOutAttribute.CityWithoutProp)
                .Include(x => x.AddressWithAttribute.StreetWithProp)
                .Include(x => x.AddressWithOutAttribute.StreetWithProp).Exclude(x => x.PropWithoutAttribute))
            .Build();
        
        // Assert
        query.Filter.Should().Be("eq(PropWithoutAttribute, 'abc')");
        query.Select.Should().Be("IamAJsonTag,Address.CityWithoutProp,AddressWithOutAttribute.CityWithoutProp,Address.Street,AddressWithOutAttribute.Street,-PropWithoutAttribute");
        query.Order.Should().Be("-IamAJsonTag,PropWithoutAttribute");
    }
}