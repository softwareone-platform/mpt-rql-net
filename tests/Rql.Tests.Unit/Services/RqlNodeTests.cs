using FluentAssertions;
using Moq;
using Mpt.Rql;
using Mpt.Rql.Abstractions;
using Xunit;

namespace Rql.Tests.Unit.Services;

public class RqlNodeTests
{
    private static IRqlPropertyInfo MakeProp(string name)
    {
        var prop = new Mock<IRqlPropertyInfo>();
        prop.SetupGet(p => p.Name).Returns(name);
        return prop.Object;
    }

    [Fact]
    public void IsIncluded_RootNode_DefaultSelect_IsTrue()
    {
        // Arrange
        var root = RqlNode.MakeRoot();

        // Act
        var included = root.IsIncluded;

        // Assert
        included.Should().BeTrue();
    }

    [Fact]
    public void IsIncluded_IncludeSelect_IsTrue()
    {
        // Arrange
        var root = RqlNode.MakeRoot();
        var child = root.IncludeChild(MakeProp("p"), IncludeReasons.Select);

        // Act
        var included = child.IsIncluded;

        // Assert
        included.Should().BeTrue();
    }

    [Fact]
    public void IsIncluded_IncludeDefault_ExcludeNone_IsTrue()
    {
        // Arrange
        var root = RqlNode.MakeRoot();
        var child = root.IncludeChild(MakeProp("p"), IncludeReasons.Default);

        // Act
        var included = child.IsIncluded;

        // Assert
        included.Should().BeTrue();
    }

    [Fact]
    public void IsIncluded_IncludeDefault_ExcludeDefault_IsTrue()
    {
        // Arrange
        var root = RqlNode.MakeRoot();
        var child = root.IncludeChild(MakeProp("p"), IncludeReasons.Default);
        child.AddExcludeReason(ExcludeReasons.Default);

        // Act
        var included = child.IsIncluded;

        // Assert
        included.Should().BeTrue();
    }

    [Fact]
    public void IsIncluded_IncludeDefault_ExcludeUnselected_IsFalse()
    {
        // Arrange
        var root = RqlNode.MakeRoot();
        var child = root.IncludeChild(MakeProp("p"), IncludeReasons.Default);
        child.AddExcludeReason(ExcludeReasons.Unselected);

        // Act
        var included = child.IsIncluded;

        // Assert
        included.Should().BeFalse();
    }

    [Fact]
    public void IsIncluded_IncludeNone_IsFalse()
    {
        // Arrange
        var root = RqlNode.MakeRoot();
        var child = root.ExcludeChild(MakeProp("p"), ExcludeReasons.Default);

        // Act
        var included = child.IsIncluded;

        // Assert
        included.Should().BeFalse();
    }

    [Fact]
    public void IsIncluded_IncludeFilter_IsTrue()
    {
        // Arrange
        var root = RqlNode.MakeRoot();
        var child = root.IncludeChild(MakeProp("p"), IncludeReasons.Filter);

        // Act
        var included = child.IsIncluded;

        // Assert
        included.Should().BeTrue();
    }

    [Fact]
    public void IsIncluded_IncludeOrder_IsTrue()
    {
        // Arrange
        var root = RqlNode.MakeRoot();
        var child = root.IncludeChild(MakeProp("p"), IncludeReasons.Order);

        // Act
        var included = child.IsIncluded;

        // Assert
        included.Should().BeTrue();
    }

    [Fact]
    public void IsIncluded_IncludeForced_IsTrue()
    {
        // Arrange
        var root = RqlNode.MakeRoot();
        var child = root.IncludeChild(MakeProp("p"), IncludeReasons.Forced);

        // Act
        var included = child.IsIncluded;

        // Assert
        included.Should().BeTrue();
    }
}
