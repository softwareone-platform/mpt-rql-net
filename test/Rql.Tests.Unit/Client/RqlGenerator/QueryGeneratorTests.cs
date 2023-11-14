using FluentAssertions;
using Rql.Tests.Unit.Client.Samples;
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
        var rql = _builderProvider.GetBuilder<User>().Build();

        // Assert
        rql.Select.Should().Be(default);
        rql.Order.Should().Be(default);
        rql.Filter.Should().Be(default);
    }

    [Fact]
    public void WhenQueryIsEmptyButHasSelect_ThenOnlySelectIsGenerated()
    {
        // Arrange & Act
        var rql = _builderProvider.GetBuilder<User>()
            .Select(e => e.Include(e => e.FirstName))
            .Build();

        // Assert
        rql.Select.Should().Be("firstName");
        rql.Order.Should().Be(default);
        rql.Filter.Should().Be(default);
    }

    [Fact]
    public void WhenSelectIsEmptyButHasSelect_ThenOnlyOrderIsGenerated()
    {
        // Arrange & Act
        var rql = _builderProvider.GetBuilder<User>()
            .OrderBy(e => e.FirstName)
            .Build();

        // Assert
        rql.Order.Should().Be("firstName");
        rql.Select.Should().Be(default);
        rql.Filter.Should().Be(default);
    }
    
    [Fact]
    public void WhenQueryIsNotEmptyAndSelectAndPagingAreNotDefined_ThenQueryIsGeneratedInRql()
    {
        // Arrange & Act
        var rql = _builderProvider.GetBuilder<User>()
            .Where(context => context.Eq(x => x.HomeAddress.Street, "abc"))
            .Build();

        // Assert
        rql.Filter.Should().Be("eq(homeAddress.street,'abc')");
        rql.Select.Should().Be(default);
        rql.Order.Should().Be(default);
    }

    [Fact]
    public void WhenQueryAndSelect_ThenQueryIsGeneratedInRql()
    {
        // Arrange & Act
        var query = _builderProvider.GetBuilder<User>()
            .Where(context => context.Eq(x => x.HomeAddress.Street, "abc"))
            .Select(c=>c.Exclude(x=>x.LastName, x => x.Id))
            .Build();

        // Assert
        query.Filter.Should().Be("eq(homeAddress.street,'abc')");
        query.Select.Should().Be("-lastName,-id");
    }

    [Fact]
    public void WhenQueryAndSelectAreNotEmptyAndShortSyntax_ThenAreGeneratedInRql()
    {
        // Arrange & Act
        var rql = _builderProvider.GetBuilder<User>()
                .Where( context => context.Eq(x => x.HomeAddress.Street, "abc"))
                .Select(context => context.Include(x => x.HomeAddress).Exclude(x => x.OfficeAddress))
                .Build();

        // Assert
        rql.Filter.Should().Be("eq(homeAddress.street,'abc')");
        rql.Select.Should().Be("homeAddress,-officeAddress");
    }

  
    [Fact]
    public void WhenQueryAndSelectAreNotEmptyIsNotDefined_ThenAreGeneratedInRql()
    {
        // Arrange & Act
        var query = _builderProvider.GetBuilder<User>()
            .Where(context => context.Eq(x => x.HomeAddress.Street, "abc"))
            .OrderByDescending(x => x.FirstName).ThenBy(x => x.LastName)
            .Select(context => context.Include(x => x.HomeAddress).Exclude(x => x.OfficeAddress))
            .Build();
        
        // Assert
        query.Filter.Should().Be("eq(homeAddress.street,'abc')");
        query.Select.Should().Be("homeAddress,-officeAddress");
        query.Order.Should().Be("-firstName,lastName");
    }

    [Fact]
    public void WhenQuerySelectOrderAndPaging_ThenAreGeneratedInRql()
    {
        // Arrange & Act
        var query = _builderProvider.GetBuilder<User>()
            .Where(context => context.Eq(x => x.HomeAddress.Street, "abc"))
            .OrderByDescending(x => x.FirstName).ThenBy(x => x.LastName)
            .Select(context => context.Include(x => x.HomeAddress).Exclude(x => x.OfficeAddress))
            .Build();

        // Assert
        query.Filter.Should().Be("eq(homeAddress.street,'abc')");
        query.Select.Should().Be("homeAddress,-officeAddress");
        query.Order.Should().Be("-firstName,lastName");
    }

    [Fact]
    public void WhenQueryingWithJsonAttributes_ThenJsonNameIsReturnedIfExistsInRql()
    {
        // Arrange & Act
        var query = _builderProvider.GetBuilder<ExampleWithJson>()
            .Where(context => context.Eq(x => x.PropWithoutAttribute, "abc"))
            .OrderByDescending(x => x.PropWithAttribute).ThenBy(x => x.PropWithoutAttribute)
            .Select(context => context.Include(x => x.PropWithAttribute)
                .Include(x => x.AddressWithAttribute.CityWithoutProp)
                .Include(x => x.AddressWithOutAttribute.CityWithoutProp)
                .Include(x => x.AddressWithAttribute.StreetWithProp)
                .Include(x => x.AddressWithOutAttribute.StreetWithProp).Exclude(x => x.PropWithoutAttribute))
            .Build();
        
        // Assert
        query.Filter.Should().Be("eq(propWithoutAttribute,'abc')");
        query.Select.Should().Be("IamAJsonTag,Address.cityWithoutProp,addressWithOutAttribute.cityWithoutProp,Address.Street,addressWithOutAttribute.Street,-propWithoutAttribute");
        query.Order.Should().Be("-IamAJsonTag,propWithoutAttribute");
    }
}