using FluentAssertions;
using Rql.Tests.Unit.Client.Models;
using System.Linq.Expressions;
using SoftwareOne.Rql.Client;
using Xunit;

namespace Rql.Tests.Unit.Client;

public class PropertyVisitorTests
{
    private readonly PropertyVisitor _propertyVisitor;

    public PropertyVisitorTests()
    {
        _propertyVisitor = new PropertyVisitor();
    }
    [Fact]
    public void GetPath_WhenProperty_ThenNameIsReturned()
    {
        // Arrange
        Expression<Func<User, string>> xx = x => x.FirstName;

        // Act
        var result = _propertyVisitor.GetPath(xx);

        // Assert
        result.Should().Be("FirstName");
    }

    [Fact]
    public void GetPath_WhenNestedProperty_ThenNameIsReturned()
    {
        // Arrange
        Expression<Func<User, string>> xx = x => x.HomeAddress.Street;

        // Act
        var result =_propertyVisitor.GetPath(xx);

        // Assert
        result.Should().Be("HomeAddress.Street");
    }

    [Fact]
    public void GetPath_WhenMethod_ThenExceptionIsThrown()
    {
        // Arrange 
        Expression<Func<User, string>> xx = x => x.GetName();

        // Act & Assert
        Assert.Throws<InvalidDefinitionException>(() => _propertyVisitor.GetPath(xx));
    }

    [Fact]
    public void GetPath_WhenMethodOnProp_ThenOnlyNameIsReturned()
    {
        // Arrange 
        Expression<Func<User, string>> xx = x => x.FirstName.ToUpper();

        // Act
        var result = _propertyVisitor.GetPath(xx);

        // Assert
        result.Should().Be("FirstName");
    }

    [Fact]
    public void GetPath_WhenOtherMethodOnProp_ThenOnlyNameIsReturned()
    {
        // Arrange 
        Expression<Func<User, bool>> xx = x => string.IsNullOrWhiteSpace(x.FirstName);

        // Act
        var result = _propertyVisitor.GetPath(xx);

        // Act & Assert
        result.Should().Be("FirstName");
    }
}