using Rql.Tests.Integration.Tests.Functionality.Utility;
using Xunit;

namespace Rql.Tests.Integration.Tests.Functionality;

public class BasicSelectTests
{
    private readonly ProductShapeTestExecutor _testExecutor;

    public BasicSelectTests()
    {
        _testExecutor = new ProductShapeTestExecutor();
    }

    [Fact]
    public void Shape_NoChange() => _testExecutor.ShapeMatch(t =>
    {
        t.HiddenCollection = null!;
    }, string.Empty);

    [Fact]
    public void Shape_HiddenCollection_Included() => _testExecutor.ShapeMatch(_ => { }, "HiddenCollection");

    [Fact]
    public void Shape_Collection_Excluded() => _testExecutor.ShapeMatch(t =>
    {
        t.Collection = null!;
        t.HiddenCollection = null!;
    }, "-Collection");

    [Fact]
    public void Shape_Reference_Excluded() => _testExecutor.ShapeMatch(t =>
    {
        t.Reference = null!;
        t.HiddenCollection = null!;
    }, "-Reference");
}