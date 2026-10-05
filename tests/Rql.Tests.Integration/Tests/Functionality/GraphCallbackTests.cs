using FluentAssertions;
using Mpt.Rql;
using Rql.Tests.Integration.Core;
using Xunit;

namespace Rql.Tests.Integration.Tests.Functionality;

public class GraphCallbackTests
{
    private readonly IRqlQueryable<Product, ShapedProduct> _rql = RqlFactory.Make<Product, ShapedProduct>(services => { }, rql =>
    {
        rql.Settings.Select.Implicit = RqlSelectModes.Core | RqlSelectModes.Primitive | RqlSelectModes.Reference;
        rql.Settings.Select.Explicit = RqlSelectModes.All;
        rql.Settings.Select.MaxDepth = 10;
    });

    [Theory]
    [InlineData(null)]
    [InlineData("name")]
    public void Hiding_Property_LeavesItOutOfTheProjection(string? select)
    {
        // Act
        var result = _rql.Transform(ProductRepository.Query(), new RqlRequest { Select = select }, options =>
            options.OnGraphBuilt(graph => Child(graph, "name").SetReasons(IncludeReasons.None, ExcludeReasons.Invisible)));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Query.ToList().Should().NotBeEmpty().And.OnlyContain(t => t.Name == null && t.Id > 0);
        Child(result.Graph, "name").IsIncluded.Should().BeFalse();
    }

    [Fact]
    public void Restoring_DeselectedProperty_ProjectsIt()
    {
        // Act
        var result = _rql.Transform(ProductRepository.Query(), new RqlRequest { Select = "-name" }, options =>
            options.OnGraphBuilt(graph => Child(graph, "name").SetReasons(IncludeReasons.Default, ExcludeReasons.None)));

        // Assert
        result.Query.ToList().Should().NotBeEmpty().And.OnlyContain(t => t.Name != null);
    }

    [Fact]
    public void Callbacks_LeaveTheGraphToBeProjectedAsIs()
    {
        // Act — reference is deselected, so its children exist but are excluded
        var result = _rql.Transform(ProductRepository.Query(), new RqlRequest { Select = "-reference" }, options =>
            options.OnGraphBuilt(graph =>
            {
                var reference = Child(graph, "reference");
                reference.SetReasons(IncludeReasons.Default, ExcludeReasons.None);
                Child(reference, "id").SetReasons(IncludeReasons.Default, ExcludeReasons.None);
            }));

        // Assert
        result.Query.ToList().Should().NotBeEmpty()
            .And.OnlyContain(t => t.Reference != null && t.Reference.Id == t.Id && t.Reference.Name == null);
    }

    [Fact]
    public void Callbacks_RunInRegistrationOrder_OnTheGraphBuiltFromTheRequest()
    {
        // Arrange
        var calls = new List<string>();
        IncludeReasons? nameReasons = null;

        // Act
        _rql.Transform(ProductRepository.Query(), new RqlRequest { Select = "name" }, options =>
        {
            options.OnGraphBuilt(graph =>
            {
                nameReasons = Child(graph, "name").IncludeReason;
                calls.Add("first");
            });
            options.OnGraphBuilt(_ => calls.Add("second"));
        });

        // Assert
        calls.Should().Equal("first", "second");
        nameReasons.Should().NotBeNull();
        nameReasons!.Value.Should().HaveFlag(IncludeReasons.Select);
    }

    [Fact]
    public void BuildGraph_RunsGraphCallbacks()
    {
        // Act
        var response = _rql.BuildGraph(new RqlRequest(), options =>
            options.OnGraphBuilt(graph => Child(graph, "name").SetReasons(IncludeReasons.None, ExcludeReasons.Invisible)));

        // Assert
        Child(response.Graph, "name").ExcludeReason.Should().Be(ExcludeReasons.Invisible);
        Child(response.Graph, "name").IsIncluded.Should().BeFalse();
    }

    [Fact]
    public void Settings_ApplyToTheirCallOnly()
    {
        // Act
        var configured = _rql.Transform(ProductRepository.Query(), new RqlRequest(), options => options.Settings.Select.Explicit = RqlSelectModes.None);
        var unconfigured = _rql.Transform(ProductRepository.Query(), new RqlRequest());

        // Assert
        configured.Query.ToList().Should().OnlyContain(t => t.Name == null);
        unconfigured.Query.ToList().Should().OnlyContain(t => t.Name != null);
    }

    [Fact]
    public void SetReasons_AfterTransform_LeavesTheQueryUnchanged()
    {
        // Arrange
        var result = _rql.Transform(ProductRepository.Query(), new RqlRequest());

        // Act
        Child(result.Graph, "name").SetReasons(IncludeReasons.None, ExcludeReasons.Invisible);

        // Assert
        result.Query.ToList().Should().NotBeEmpty().And.OnlyContain(t => t.Name != null);
    }

    private static IRqlNode Child(IRqlNode node, string name)
    {
        node.TryGetChild(name, out var child).Should().BeTrue($"'{name}' is in the graph");
        return child!;
    }
}
