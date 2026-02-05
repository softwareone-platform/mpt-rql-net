using FluentAssertions;
using Rql.Tests.Integration.Core;
using Mpt.Rql;
using Xunit;

namespace Rql.Tests.Integration.Tests.Functionality;

public class BasicGraphBuilderTests
{
    private readonly IRqlQueryable<Product, ShapedProduct> _rql;

    public BasicGraphBuilderTests()
    {
        _rql = RqlFactory.Make<Product, ShapedProduct>(services => { }, rql =>
        {
            rql.Settings.Select.Implicit = RqlSelectModes.Core | RqlSelectModes.Primitive | RqlSelectModes.Reference;
            rql.Settings.Select.Explicit = RqlSelectModes.All;
            rql.Settings.Select.MaxDepth = 10;
        });
    }

    [Fact]
    public void Simple_Request_Should_BuildGraph()
    {
        // Act
        var graphResponse = _rql.BuildGraph(new RqlRequest { });

        // Assert
        graphResponse.IsSuccess.Should().BeTrue();
    }
}