using FluentAssertions;
using Rql.Tests.Integration.Tests.Functionality.Utility;
using Xunit;

namespace Rql.Tests.Integration.Tests.Functionality;

public class BasicGraphBuilderTests
{
    public BasicGraphBuilderTests()
    {
        TestExecutor = new ProductShapeTestExecutor();
    }

    protected ProductShapeTestExecutor TestExecutor { get; set; }

    [Fact]
    public void Simple_Request_Should_BuildGraph()
    {
        var graphResponse = TestExecutor.Rql.BuildGraph(new Mpt.Rql.RqlRequest { });
        graphResponse.IsSuccess.Should().BeTrue();
    }
}