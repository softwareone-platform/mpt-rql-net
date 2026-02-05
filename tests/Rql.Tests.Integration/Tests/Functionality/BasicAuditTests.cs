using FluentAssertions;
using Rql.Tests.Integration.Core;
using Mpt.Rql;
using Mpt.Rql.Abstractions;
using Xunit;

namespace Rql.Tests.Integration.Tests.Functionality;

public class BasicAuditTests
{
    private readonly IRqlQueryable<Product, ShapedProduct> _rql;

    public BasicAuditTests()
    {
        _rql = RqlFactory.Make<Product, ShapedProduct>(services => { }, rql =>
        {
            rql.Settings.Select.Implicit = RqlSelectModes.Core | RqlSelectModes.Primitive | RqlSelectModes.Reference;
            rql.Settings.Select.Explicit = RqlSelectModes.All;
            rql.Settings.Select.MaxDepth = 10;
        });
    }

    [Fact]
    public void Audit_Omitted_Default()
    {
        // Arrange
        var testData = ProductRepository.Query();

        // Act
        var result = _rql.Transform(testData, new RqlRequest { });

        // Assert
        result.Graph.TryGetChild("hiddenCollection", out var hidden);
        hidden.Should().NotBeNull();
        hidden!.ExcludeReason.Should().HaveFlag(ExcludeReasons.Default);
    }

    [Fact]
    public void Audit_Omitted_Extra()
    {
        // Arrange
        var testData = ProductRepository.Query();
        var extra = new List<string> { "category", "price", "name" };

        // Act
        var result = _rql.Transform(testData, new RqlRequest { Select = string.Join(',', extra.Select(s => $"-{s}")) });

        // Assert
        result.Graph.TryGetChild("hiddenCollection", out var hidden);
        hidden.Should().NotBeNull();
        hidden!.ExcludeReason.Should().HaveFlag(ExcludeReasons.Default);

        result.Graph.TryGetChild("category", out var category);
        category.Should().NotBeNull();
        category!.ExcludeReason.Should().HaveFlag(ExcludeReasons.Unselected);

        result.Graph.TryGetChild("price", out var price);
        price.Should().NotBeNull();
        price!.ExcludeReason.Should().HaveFlag(ExcludeReasons.Unselected);

        result.Graph.TryGetChild("name", out var name);
        name.Should().NotBeNull();
        name!.ExcludeReason.Should().HaveFlag(ExcludeReasons.Unselected);
    }
}