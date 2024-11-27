using FluentAssertions;
using SoftwareOne.Rql.Linq.Client.Builder.Select;
using SoftwareOne.Rql.Linq.Client.Core;
using SoftwareOne.Rql.Linq.Client.Generator;
using SoftwareOne.Rql.Linq.Core.Metadata;
using SoftwareOne.Rql.Linq.UnitTests.Client.Samples;
using Xunit;

namespace SoftwareOne.Rql.Linq.UnitTests.Client.RqlGenerator;

public class SelectGeneratorTests
{
    [Fact]
    public void WhenSelect_ThenGenerated()
    {
        // Arrange
        var context = new SelectContext<User>().Include(x => x.HomeAddress.Street).Exclude(x => x.HomeAddress, x => x.Id).Include(e => e.FirstName, e => e.LastName);
        ISelectDefinitionProvider definition = ((SelectContext<User>)context);

        // Act
        var result = new SelectGenerator(new PropertyVisitor(new PropertyNameProvider())).Generate(definition);

        // Assert
        result.Should().Be("homeAddress.street,firstName,lastName,-homeAddress,-id");
    }
}