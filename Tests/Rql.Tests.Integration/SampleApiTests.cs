using Microsoft.AspNetCore.Mvc.Testing;
using Rql.Tests.Common;
using Rql.Tests.Integration.Fixtures;
using Rql.Tests.Integration.Service;
using Xunit;

namespace Rql.Tests.Integration;

public class SampleApiTests : IClassFixture<SampleApiInstanceFixture>
{
    private readonly TestExecutor _testExecutor;

    public SampleApiTests()
    {
        _testExecutor = new TestExecutor();
    }

    [Fact]
    public async Task Get_AllItems_ReturnsAtLeastSome()
    {
        await _testExecutor.Execute(t => true);
    }

    [Theory]
    [InlineData("eq(name,Jewelry Widget)")]
    [InlineData("name=Jewelry Widget")]
    [InlineData("name=eq=Jewelry Widget")]
    [InlineData("eq(name,WRONG_DATA)", false)]
    public Task Eq_Name_Equal(string query, bool isHappyFlow = true)
        => _testExecutor.Execute(t => t.Name == "Jewelry Widget", query, isHappyFlow: isHappyFlow);


    [Theory]
    [InlineData("eq(sub.name,Jewelry Widget)")]
    [InlineData("sub.name=Jewelry Widget")]
    [InlineData("sub.name=eq=Jewelry Widget")]
    [InlineData("eq(sub.name,WRONG_DATA)", false)]
    public Task Path_Name_Equal(string query, bool isHappyFlow = true)
        => _testExecutor.Execute(t => t.Sub!.Name == "Jewelry Widget", query, isHappyFlow: isHappyFlow);

    [Theory]
    [InlineData("ne(name,Jewelry Widget)")]
    [InlineData("name=ne=Jewelry Widget")]
    [InlineData("ne(name,WRONG_DATA)", false)]
    public Task Ne_Name_NotEqual(string query, bool isHappyFlow = true)
        => _testExecutor.Execute(t => t.Name != "Jewelry Widget", query, isHappyFlow: isHappyFlow);

    [Theory]
    [InlineData("gt(price,200.5)")]
    [InlineData("price=gt=200.5")]
    [InlineData("gt(price,10000)", false)]
    public Task Gt_Price_GreaterThan(string query, bool isHappyFlow = true)
        => _testExecutor.Execute(t => t.Price > 200.5M, query, isHappyFlow: isHappyFlow);

    [Theory]
    [InlineData("ge(price,129.99)")]
    [InlineData("price=ge=129.99")]
    [InlineData("ge(price,10000)", false)]
    public Task Ge_Price_GreaterThanOrEqual(string query, bool isHappyFlow = true)
        => _testExecutor.Execute(t => t.Price >= 129.99M, query, isHappyFlow: isHappyFlow);

    [Theory]
    [InlineData("lt(price,150.1)")]
    [InlineData("price=lt=150.1")]
    [InlineData("lt(price,-10000)", false)]
    public Task Lt_Price_LessThan(string query, bool isHappyFlow = true)
        => _testExecutor.Execute(t => t.Price < 150.1M, query, isHappyFlow: isHappyFlow);

    [Theory]
    [InlineData("le(price,205.15)")]
    [InlineData("price=le=205.15")]
    [InlineData("le(price,-1000)", false)]
    public Task Le_Price_LessThanOrEqual(string query, bool isHappyFlow = true)
        => _testExecutor.Execute(t => t.Price <= 205.15M, query, isHappyFlow: isHappyFlow);

    [Theory]
    [InlineData("like(name,Jewelry*)")]
    [InlineData("name=like=Jewelry*")]
    [InlineData("like(name,WRONG_DATA*)", false)]
    public Task Like_Name_StartsWith(string query, bool isHappyFlow = true)
        => _testExecutor.Execute(t => t.Name.StartsWith("Jewelry"), query, isHappyFlow: isHappyFlow);

    [Theory]
    [InlineData("like(name,*Widget)")]
    [InlineData("name=like=*Widget")]
    [InlineData("like(name,*WRONG_DATA)", false)]
    public Task Like_Name_EndsWith(string query, bool isHappyFlow = true)
        => _testExecutor.Execute(t => t.Name.EndsWith("Widget"), query, isHappyFlow: isHappyFlow);

    [Theory]
    [InlineData("like(name,*Wid*)")]
    [InlineData("name=like=*Wid*")]
    [InlineData("like(name,*WRONG_DATA*)", false)]
    public Task Like_Name_Contains(string query, bool isHappyFlow = true)
        => _testExecutor.Execute(t => t.Name.Contains("Wid"), query, isHappyFlow: isHappyFlow);

    [Theory]
    [InlineData("not(eq(id,1))")]
    [InlineData("not(eq(id,2))", false)]
    public Task Not_Name_NotContains(string query, bool isHappyFlow = true)
        => _testExecutor.Execute(t => t.Id != 1, query, isHappyFlow: isHappyFlow);

    [Theory]
    [InlineData("in(id,(1,3,6))")]
    [InlineData("in(id,(1,3))", false)]
    public Task In_Id_MatchList(string query, bool isHappyFlow = true)
    {
        var ids = new List<int> { 1, 3, 6 };
        return _testExecutor.Execute(t => ids.Contains(t.Id), query, isHappyFlow: isHappyFlow);
    }

    [Theory]
    [InlineData("out(id,(1,3,6))")]
    [InlineData("out(id,(1,3,7))", false)]
    public Task Out_Id_NotMatchList(string query, bool isHappyFlow = true)
    {
        var ids = new List<int> { 1, 3, 6 };
        return _testExecutor.Execute(t => !ids.Contains(t.Id), query, isHappyFlow: isHappyFlow);
    }

    [Theory]
    [InlineData("desc=null()")]
    [InlineData("not(eq(desc,null()))", false)]
    public Task Null_Desc_DescriptionIsNull(string query, bool isHappyFlow = true)
       => _testExecutor.Execute(t => t.Desc == null, query, isHappyFlow: isHappyFlow);

    [Theory]
    [InlineData("eq(price,null())", false)]
    public Task Null_Price_PriceIsNull(string query, bool isHappyFlow = true)
    {
        return Assert.ThrowsAnyAsync<HttpRequestException>(
            () => _testExecutor.Execute(t => t.Price == null, query, isHappyFlow: isHappyFlow));
    }

    [Theory]
    [InlineData("+category,-name")]
    [InlineData("category,-name")]
    [InlineData("-category,-name", false)]
    public Task Ordering_CategotyAsc_NameDesc(string order, bool isHappyFlow = true)
       => _testExecutor.Execute(MockProductRepository.View.OrderBy(o => o.Category).ThenByDescending(o => o.Name), order: order, isHappyFlow: isHappyFlow);

    [Theory]
    [InlineData("and(eq(id,1),eq(name,Jewelry Widget))")]
    [InlineData("and(id=1,name=Jewelry Widget)")]
    [InlineData("and(id=eq=1,name=eq=Jewelry Widget)")]
    [InlineData("and(eq(id,1),eq(id,2),eq(name,Jewelry Widget))", false)]
    public Task And_Id_Name_Equals(string query, bool isHappyFlow = true)
       => _testExecutor.Execute(t => t.Id == 1 && t.Name == "Jewelry Widget", query, isHappyFlow: isHappyFlow);

    [Theory]
    [InlineData("or(eq(id,1),eq(id,2),eq(name,Jewelry Widget))")]
    [InlineData("or(id=1,id=2,name=Jewelry Widget)")]
    [InlineData("or(id=eq=1,id=eq=2,name=eq=Jewelry Widget)")]
    [InlineData("or(eq(id,3),eq(id,5),eq(name,Jewelry Widget))", false)]
    public Task Or_Id_Name_Equals(string query, bool isHappyFlow = true)
       => _testExecutor.Execute(t => t.Id == 1 || t.Id == 2 || t.Name == "Jewelry Widget", query, isHappyFlow: isHappyFlow);
}