using Rql.Tests.Unit.Client.Models;
using SoftwareOne.Rql.Linq.Client.Dsl;
using Xunit;

namespace Rql.Tests.Unit.Client.Builder;

public class OperatorTests
{
    [Fact]
    public void BaseOperator_And_BuildProperAnd()
    {
        // Arrange & Act
        var op = new In<User, int>(x => x.Id, new List<int> { 1 }).And(new In<User, int>(x => x.Id, new List<int> { 1 }));

        // Assert
        Assert.NotNull(op);
        Assert.Equal("AndOperator { Left = In { Exp = x => x.Id, Values = System.Collections.Generic.List`1[System.Int32] }, Right = In { Exp = x => x.Id, Values = System.Collections.Generic.List`1[System.Int32] } }", op.ToString());
    }

    [Fact]
    public void BaseOperator_Or_BuildProperOr()
    {
        // Arrange & Act
        var op = new In<User, int>(x => x.Id, new List<int> { 1 }).Or(new In<User, int>(x => x.Id, new List<int> { 1 }));

        // Assert
        Assert.NotNull(op);
        Assert.Equal("OrOperator { Left = In { Exp = x => x.Id, Values = System.Collections.Generic.List`1[System.Int32] }, Right = In { Exp = x => x.Id, Values = System.Collections.Generic.List`1[System.Int32] } }", op.ToString());
    }
}