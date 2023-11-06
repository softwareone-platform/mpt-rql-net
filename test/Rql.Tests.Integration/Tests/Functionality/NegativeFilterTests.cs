using Rql.Tests.Integration.Tests.Functionality.Utility;
using Xunit;

namespace Rql.Tests.Integration.Tests.Functionality;

public class NegativeFilterTests
{
    private readonly ProductTestExecutor _testExecutor;

    public NegativeFilterTests()
    {
        _testExecutor = new ProductTestExecutor();
    }

    [Theory]
    [InlineData("any(Orders,self(abc)=1)")]
    [InlineData("any(Orders,eq(self(abc),1))")]
    public void Any_SaleDetailIds_Equals(string query)
       => _testExecutor.MustFailWithError(filter: query, errorDescription: "Invalid property path.");

    [Theory]
    [InlineData("like(self(),*Widget)")]
    public void Like_Name_EndsWith_(string query)
         => _testExecutor.MustFailWithError(filter: query, errorDescription: "Search operations work with properties only");
}