using Rql.Tests.Integration.Tests.Functionality.Utility;
using Xunit;

namespace Rql.Tests.Integration.Tests.Functionality;

public class BasicSelectTests
{
    public BasicSelectTests()
    {
        TestExecutor = new ProductShapeTestExecutor();
    }

    protected ProductShapeTestExecutor TestExecutor { get; set; }

    [Fact]
    public void Shape_NoChange() => TestExecutor.ShapeMatch(t =>
    {
        t.HiddenCollection = null!;
        t.Ignored = null!;
    }, string.Empty);

    [Fact]
    public void Shape_HiddenCollection_Included() => TestExecutor.ShapeMatch(t => { t.Ignored = null!; }, "HiddenCollection,Ignored");

    [Fact]
    public void Shape_Collection_Excluded() => TestExecutor.ShapeMatch(t =>
    {
        t.Collection = null!;
        t.HiddenCollection = null!;
        t.Ignored = null!;
    }, "-Collection");

    [Fact]
    public void Shape_Reference_Excluded() => TestExecutor.ShapeMatch(t =>
    {
        t.Reference = null!;
        t.HiddenCollection = null!;
        t.Ignored = null!;
    }, "-Reference");
}