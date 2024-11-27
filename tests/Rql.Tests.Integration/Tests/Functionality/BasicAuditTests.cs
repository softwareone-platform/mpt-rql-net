using FluentAssertions;
using Rql.Tests.Integration.Tests.Functionality.Utility;
using SoftwareOne.Rql;
using Xunit;

namespace Rql.Tests.Integration.Tests.Functionality;

public class BasicAuditTests
{
    private readonly ProductShapeTestExecutor _testExecutor;

    public BasicAuditTests()
    {
        _testExecutor = new ProductShapeTestExecutor();
    }

    [Fact]
    public void Audit_Omitted_Default()
    {
        // Arrange and Act
        var result = _testExecutor.Rql.Transform(_testExecutor.GetQuery(), new RqlRequest { });

        // Assert
        result.Graph.TryGetChild("hiddenCollection", out var hidden);
        hidden.Should().NotBeNull();
        hidden!.ExcludeReason.Should().HaveFlag(ExcludeReasons.Default);
    }

    [Fact]
    public void Audit_Omitted_Extra()
    {
        // Arrange and Act
        var extra = new List<string> { "category", "price", "name" };
        var result = _testExecutor.Rql.Transform(_testExecutor.GetQuery(), new RqlRequest { Select = string.Join(',', extra.Select(s => $"-{s}")) });

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