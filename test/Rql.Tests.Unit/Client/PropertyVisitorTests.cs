using System.Linq.Expressions;
using Xunit;
using SoftwareOne.Rql.Client;
using FluentAssertions;
using Rql.Tests.Unit.Client.Models;

namespace Rql.Tests.Unit.Client;

public class PropertyVisitorTests
{
    [Fact]
    public void GetPath_WhenProperty_ThenNameIsReturned()
    {
        // Arrange
        Expression<Func<User, string>> xx = x => x.FirstName;

        // Act
        var result = new PropertyVisitor().GetPath(xx);

        // Assert
        result.Should().Be("FirstName");
    }

    [Fact]
    public void GetPath_WhenNestedProperty_ThenNameIsReturned()
    {
        // Arrange
        Expression<Func<User, string>> xx = x => x.HomeAddress.Street;

        // Act
        var result = new PropertyVisitor().GetPath(xx);

        // Assert
        result.Should().Be("HomeAddress.Street");
    }

    [Fact]
    public void GetPath_WhenMethod_ThenExceptionIsThrown()
    {
        // Arrange 
        Expression<Func<User, string>> xx = x => x.GetName();

        // Act & Assert
        Assert.Throws<NotSupportedException>(() => new PropertyVisitor().GetPath(xx));
    }
}