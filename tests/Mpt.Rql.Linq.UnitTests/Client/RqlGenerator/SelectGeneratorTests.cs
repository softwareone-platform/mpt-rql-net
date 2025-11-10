using FluentAssertions;
using Mpt.Rql.Linq.Client.Builder.Select;
using Mpt.Rql.Linq.Client.Core;
using Mpt.Rql.Linq.Client.Generator;
using Mpt.Rql.Linq.Core.Metadata;
using Mpt.Rql.Linq.UnitTests.Client.Samples;
using Xunit;

namespace Mpt.Rql.Linq.UnitTests.Client.RqlGenerator;

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