using Xunit;

namespace Rql.Tests.Integration.Tests.Filter;

public class BasicFilterTests
{
    private readonly ProductTestExecutor _testExecutor;

    public BasicFilterTests()
    {
        _testExecutor = new ProductTestExecutor();
    }

    [Theory]
    [InlineData("eq(name,Jewelry Widget)")]
    [InlineData("name=Jewelry Widget")]
    [InlineData("eq(name,WRONG_DATA)", false)]
    public void Eq_Name_Equal(string query, bool isHappyFlow = true)
        => _testExecutor.ResultMatch(t => t.Name == "Jewelry Widget", query, isHappyFlow: isHappyFlow);


    [Theory]
    [InlineData("eq(sub.name,Jewelry Widget)")]
    [InlineData("sub.name=Jewelry Widget")]
    [InlineData("eq(sub.name,WRONG_DATA)", false)]
    public void Path_Name_Equal(string query, bool isHappyFlow = true)
        => _testExecutor.ResultMatch(t => t.Sub!.Name == "Jewelry Widget", query, isHappyFlow: isHappyFlow);

    [Theory]
    [InlineData("ne(name,Jewelry Widget)")]
    [InlineData("ne(name,WRONG_DATA)", false)]
    public void Ne_Name_NotEqual(string query, bool isHappyFlow = true)
        => _testExecutor.ResultMatch(t => t.Name != "Jewelry Widget", query, isHappyFlow: isHappyFlow);

    [Theory]
    [InlineData("gt(price,200.5)")]
    [InlineData("gt(price,10000)", false)]
    public void Gt_Price_GreaterThan(string query, bool isHappyFlow = true)
        => _testExecutor.ResultMatch(t => t.Price > 200.5M, query, isHappyFlow: isHappyFlow);

    [Theory]
    [InlineData("ge(price,129.99)")]
    [InlineData("ge(price,10000)", false)]
    public void Ge_Price_GreaterThanOrEqual(string query, bool isHappyFlow = true)
        => _testExecutor.ResultMatch(t => t.Price >= 129.99M, query, isHappyFlow: isHappyFlow);

    [Theory]
    [InlineData("lt(price,150.1)")]
    [InlineData("lt(price,-10000)", false)]
    public void Lt_Price_LessThan(string query, bool isHappyFlow = true)
        => _testExecutor.ResultMatch(t => t.Price < 150.1M, query, isHappyFlow: isHappyFlow);

    [Theory]
    [InlineData("le(price,205.15)")]
    [InlineData("le(price,-1000)", false)]
    public void Le_Price_LessThanOrEqual(string query, bool isHappyFlow = true)
        => _testExecutor.ResultMatch(t => t.Price <= 205.15M, query, isHappyFlow: isHappyFlow);

    [Theory]
    [InlineData("like(name,Jewelry*)")]
    [InlineData("like(name,WRONG_DATA*)", false)]
    public void Like_Name_StartsWith(string query, bool isHappyFlow = true)
        => _testExecutor.ResultMatch(t => t.Name.StartsWith("Jewelry"), query, isHappyFlow: isHappyFlow);

    [Theory]
    [InlineData("like(name,*Widget)")]
    [InlineData("like(name,*WRONG_DATA)", false)]
    public void Like_Name_EndsWith(string query, bool isHappyFlow = true)
        => _testExecutor.ResultMatch(t => t.Name.EndsWith("Widget"), query, isHappyFlow: isHappyFlow);

    [Theory]
    [InlineData("like(name,*Wid*)")]
    [InlineData("like(name,*WRONG_DATA*)", false)]
    public void Like_Name_Contains(string query, bool isHappyFlow = true)
        => _testExecutor.ResultMatch(t => t.Name.Contains("Wid"), query, isHappyFlow: isHappyFlow);

    [Theory]
    [InlineData("not(eq(id,1))")]
    [InlineData("not(eq(id,2))", false)]
    public void Not_Name_NotContains(string query, bool isHappyFlow = true)
        => _testExecutor.ResultMatch(t => t.Id != 1, query, isHappyFlow: isHappyFlow);

    [Theory]
    [InlineData("in(id,(1,3,6))")]
    [InlineData("in(id,(1,3))", false)]
    public void In_Id_MatchList(string query, bool isHappyFlow = true)
    {
        var ids = new List<int> { 1, 3, 6 };
        _testExecutor.ResultMatch(t => ids.Contains(t.Id), query, isHappyFlow: isHappyFlow);
    }

    [Theory]
    [InlineData("out(id,(1,3,6))")]
    [InlineData("out(id,(1,3,7))", false)]
    public void Out_Id_NotMatchList(string query, bool isHappyFlow = true)
    {
        var ids = new List<int> { 1, 3, 6 };
        _testExecutor.ResultMatch(t => !ids.Contains(t.Id), query, isHappyFlow: isHappyFlow);
    }

    [Theory]
    [InlineData("desc=null()")]
    [InlineData("not(eq(desc,null()))", false)]
    public void Null_Desc_DescriptionIsNull(string query, bool isHappyFlow = true)
       => _testExecutor.ResultMatch(t => t.Desc == null, query, isHappyFlow: isHappyFlow);

    [Theory]
    [InlineData("and(eq(id,1),eq(name,Jewelry Widget))")]
    [InlineData("and(id=1,name=Jewelry Widget)")]
    [InlineData("and(eq(id,1),eq(id,2),eq(name,Jewelry Widget))", false)]
    public void And_Id_Name_Equals(string query, bool isHappyFlow = true)
       => _testExecutor.ResultMatch(t => t.Id == 1 && t.Name == "Jewelry Widget", query, isHappyFlow: isHappyFlow);

    [Theory]
    [InlineData("or(eq(id,1),eq(id,2),eq(name,Jewelry Widget))")]
    [InlineData("or(id=1,id=2,name=Jewelry Widget)")]
    [InlineData("or(eq(id,3),eq(id,5),eq(name,Jewelry Widget))", false)]
    public void Or_Id_Name_Equals(string query, bool isHappyFlow = true)
       => _testExecutor.ResultMatch(t => t.Id == 1 || t.Id == 2 || t.Name == "Jewelry Widget", query, isHappyFlow: isHappyFlow);
}