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
        _testExecutor.Rql.Transform(_testExecutor.GetQuery(), new RqlRequest { }, out var rqlAudit);
        var shouldBe = new List<string>() { "hiddenCollection" };

        // Assert
        shouldBe.Should().BeEquivalentTo(rqlAudit.Omitted);
    }

    [Fact]
    public void Audit_Omitted_Extra()
    {
        // Arrange and Act
        var extra = new List<string> { "category", "price", "name" };
        _testExecutor.Rql.Transform(_testExecutor.GetQuery(), new RqlRequest { Select = string.Join(',', extra.Select(s => $"-{s}")) }, out var rqlAudit);
        var shouldBe = new List<string>() { "hiddenCollection" };
        shouldBe.AddRange(extra);

        // Assert
        shouldBe.Should().BeEquivalentTo(rqlAudit.Omitted);
    }
}