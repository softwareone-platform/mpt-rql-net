using FluentAssertions;
using Rql.Tests.Unit.Client.Models;
using Rql.Tests.Unit.Client.Samples;
using SoftwareOne.Rql.Client;
using SoftwareOne.Rql.Linq.Client;
using SoftwareOne.Rql.Linq.Client.Select;
using SoftwareOne.Rql.Linq.Core.Metadata;
using Xunit;

namespace Rql.Tests.Unit.Client.RqlGenerator;

public class SelectGeneratorTests
{
    [Fact]
    public void WhenSelect_ThenGenerated()
    {
        // Arrange
        var context = new SelectContext<User>().Include(x => x.HomeAddress.Street).Exclude(x => x.FirstName);
        ISelectDefinitionProvider definition  = ((SelectContext<User>)context);

        // Act
        var result = new SelectGenerator(new PropertyVisitor(new PropertyNameProvider())).Generate(definition);

        // Assert
        result.Should().Be("HomeAddress.Street,-FirstName");
    }

    ///TODO: to be fixed
    //[Fact]
    //public void WhenExternalSelect_ThenException()
    //{
    //    // Arrange
    //    var selects = new List<ISelect> { new SampleSelect() };
    //    var fields = new SelectFields(selects, selects);

    //    // Act & Assert
    //    Assert.Throws<InvalidDefinitionException>(() =>
    //        new SelectGenerator(new PropertyVisitor(new PropertyNameProvider())).Generate(fields));
    //}
}