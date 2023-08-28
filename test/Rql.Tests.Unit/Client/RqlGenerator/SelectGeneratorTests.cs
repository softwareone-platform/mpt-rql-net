using FluentAssertions;
using Rql.Tests.Unit.Client.Models;
using SoftwareOne.Rql.Linq.Client.Builder.Select;
using SoftwareOne.Rql.Linq.Client.RqlGenerator;
using Xunit;

namespace Rql.Tests.Unit.Client.RqlGenerator;

public class SelectGeneratorTests
{
    [Fact]
    public void WhenSelect_ThenGenerated()
    {
        // Arrange
        var def = new SelectContext<User>().Include(x => x.HomeAddress.Street).Exclude(x => x.FirstName).GetDefinition();

        // Act
        var result = new SelectGenerator().Generate(def);

        // Assert
        result.Should().Be("select=HomeAddress.Street,-FirstName");
    }

}