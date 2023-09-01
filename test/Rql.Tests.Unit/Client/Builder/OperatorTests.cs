using SoftwareOne.Rql.Linq.Client.Dsl;
using Xunit;

namespace Rql.Tests.Unit.Client.Builder;

public class OperatorTests
{
    [Fact]
    public void BaseOperator_And_BuildProperAnd()
    {
        // Arrange & Act
        var op = new EmptyOperator().And(new EmptyOperator());

        // Assert
        Assert.NotNull(op);
        Assert.Equal("AndOperator { Left = EmptyOperator { }, Right = EmptyOperator { } }", op.ToString());
    }

    [Fact]
    public void BaseOperator_Or_BuildProperOr()
    {
        // Arrange & Act
        var op = new EmptyOperator().Or(new EmptyOperator());

        // Assert
        Assert.NotNull(op);
        Assert.Equal("OrOperator { Left = EmptyOperator { }, Right = EmptyOperator { } }", op.ToString());
    }
}