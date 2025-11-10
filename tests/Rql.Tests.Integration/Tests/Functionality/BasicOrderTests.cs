using Rql.Tests.Integration.Tests.Functionality.Utility;
using Xunit;

namespace Rql.Tests.Integration.Tests.Functionality;

public class BasicOrderTests
{
    private readonly ProductTestExecutor _testExecutor;

    public BasicOrderTests()
    {
        _testExecutor = new ProductTestExecutor();
    }

    [Theory]
    [InlineData("+category,+id,-name")]
    [InlineData("category,id,-name")]
    [InlineData("-category,-name", false)]
    public void Ordering_CategoryAsc_NameDesc(string order, bool isHappyFlow = true)
       => _testExecutor.ResultMatch(q => q.OrderBy(o => o.Category).ThenBy(o => o.Id).ThenByDescending(o => o.Name), order: order, isHappyFlow: isHappyFlow);

    [Fact]
    public void Ordering_By_IsIgnored_ThrowsException()
       => _testExecutor.MustFailWithError(order: "ignored", errorMessage: "Invalid property path.");
}