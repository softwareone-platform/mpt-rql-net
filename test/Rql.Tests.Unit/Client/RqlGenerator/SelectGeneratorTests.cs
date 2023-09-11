using FluentAssertions;
using Rql.Tests.Unit.Client.Models;
using SoftwareOne.Rql.Linq.Client.Builder.Select;
using SoftwareOne.Rql.Linq.Client.Core;
using SoftwareOne.Rql.Linq.Client.Generator;
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
        var context = new SelectContext<User>().Include(x => x.HomeAddress.Street).Exclude(x => x.HomeAddress, x => x.Id).Include(e => e.FirstName, e => e.LastName);
        ISelectDefinitionProvider definition = ((SelectContext<User>)context);

        // Act
        var result = new SelectGenerator(new PropertyVisitor(new PropertyNameProvider())).Generate(definition);

        // Assert
        result.Should().Be("HomeAddress.Street,FirstName,LastName,-HomeAddress,-Id");
    }
}