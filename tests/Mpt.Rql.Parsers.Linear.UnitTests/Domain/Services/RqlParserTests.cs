using Mpt.Rql.Abstractions;
using Mpt.Rql.Abstractions.Argument;
using Mpt.Rql.Abstractions.Argument.Pointer;
using Mpt.Rql.Abstractions.Binary;
using Mpt.Rql.Abstractions.Collection;
using Mpt.Rql.Abstractions.Exception;
using Mpt.Rql.Abstractions.Group;
using Mpt.Rql.Abstractions.Unary;
using Mpt.Rql.Parsers.Linear.Domain.Services;
using Xunit;

namespace Rql.Tests.Unit.Parsers.Linear.Domain.Services;

public class RqlParserTests
{
    private readonly RqlParser _sut;

    public RqlParserTests()
    {
        _sut = new RqlParser();
    }

    [Theory]
    [InlineData("field1=value1&field2=value2|field3=value3&field4=value4")]
    public void Parse_WithAndOrAnd_ReturnsOr(string query)
    {
        // Act
        var actualResult = _sut.Parse(query);

        // Assert
        var or = Assert.IsType<RqlOr>(actualResult);
        var andLeft = Assert.IsType<RqlAnd>(or.Items?[0]);
        var andRight = Assert.IsType<RqlAnd>(or.Items?[1]);
        Assert.IsType<RqlEqual>(andLeft.Items?[0]);
        Assert.IsType<RqlEqual>(andLeft.Items?[1]);
        Assert.IsType<RqlEqual>(andRight.Items?[0]);
        Assert.IsType<RqlEqual>(andRight.Items?[1]);
    }

    [Theory]
    [InlineData("field1=value1|field2=value2&field3=value3|field4=value4")]
    public void Parse_WithOrAndOr_ReturnsEqualWithAndWithEqualRqlExpressions(string query)
    {
        // Act
        var actualResult = _sut.Parse(query);

        // Assert
        var or = Assert.IsType<RqlOr>(actualResult);
        Assert.IsType<RqlEqual>(or.Items?[0]);
        Assert.IsType<RqlAnd>(or.Items?[1]);
        Assert.IsType<RqlEqual>(or.Items?[2]);
    }

    [Theory]
    [InlineData("(field1=value1|field2=value2)&(field3=value3|field4=value4)")]
    public void Parse_WithOrBracketsAndOrBrackets_ReturnsAndWithOrSubRqlExpressions(string query)
    {
        // Act
        var actualResult = _sut.Parse(query);

        // Assert
        var and = Assert.IsType<RqlAnd>(actualResult);
        Assert.IsType<RqlOr>(and.Items?[0]);
        Assert.IsType<RqlOr>(and.Items?[1]);
    }

    [Theory]
    [InlineData("status=processing")]
    [InlineData("eq(status,processing)")]
    public void Parse_WithEqualsQuery_ReturnsValidResult(string query)
    {
        // Act
        var actualResult = _sut.Parse(query);

        // Assert
        var eq = Assert.IsType<RqlEqual>(actualResult.Items![0]);
        var member = Assert.IsType<RqlConstant>(eq.Left);
        Assert.Equal("status", member.Value);
        var constant = Assert.IsType<RqlConstant>(eq.Right);
        Assert.Equal("processing", constant.Value);
    }

    [Theory]
    [InlineData("status=eq=processing")]
    public void Parse_WithInvalidTooManyDelimiters_ThrowsRqlExpressionMapperException(string query)
    {
        // Act and Assert
        Assert.Throws<RqlParserException>(() => _sut.Parse(query));
    }

    [Theory]
    [InlineData("ne(status,processing)")]
    public void Parse_WithNotEqualsQuery_ReturnsValidResult(string query)
    {
        // Act
        var actualResult = _sut.Parse(query);

        // Assert
        var neq = Assert.IsType<RqlNotEqual>(actualResult.Items![0]);
        var member = Assert.IsType<RqlConstant>(neq.Left);
        Assert.Equal("status", member.Value);
        var constant = Assert.IsType<RqlConstant>(neq.Right);
        Assert.Equal("processing", constant.Value);
    }

    [Theory]
    [InlineData("ne(status,(processing,otherStatus))")]
    public void Parse_WithNotEqualsMultipleQuery_ReturnsValidResult(string query)
    {
        // Act
        var actualResult = _sut.Parse(query);

        // Assert
        var neq = Assert.IsType<RqlNotEqual>(actualResult.Items![0]);
        var member = Assert.IsType<RqlConstant>(neq.Left);
        Assert.Equal("status", member.Value);
        Assert.IsAssignableFrom<RqlGroup>(neq.Right);
    }


    [Theory]
    [InlineData("not(status=processing)")]
    public void Parse_WithNotQuery_ReturnsValidResult(string query)
    {
        // Act
        var actualResult = _sut.Parse(query);

        // Assert
        var not = Assert.IsType<RqlNot>(actualResult.Items![0]);
        var member = Assert.IsType<RqlEqual>(not.Nested);
        var innerMember = Assert.IsType<RqlConstant>(member.Left);
        Assert.Equal("status", innerMember.Value);
        var constant = Assert.IsType<RqlConstant>(member.Right);
        Assert.Equal("processing", constant.Value);
    }

    [Theory]
    [InlineData("not(status(processing,otherValue))")]
    public void Parse_WithNotQueryMultiple_ReturnsValidRqlGenericGroupResult(string query)
    {
        // Act
        var actualResult = _sut.Parse(query);

        // Assert
        var neq = Assert.IsType<RqlNot>(actualResult.Items![0]);
        var nested = Assert.IsType<RqlGenericGroup>(neq.Nested);
        Assert.Equal("status", nested.Name);
        var innerMember1 = Assert.IsType<RqlConstant>(nested.Items?[0]);
        Assert.Equal("processing", innerMember1.Value);
        var innerMember2 = Assert.IsType<RqlConstant>(nested.Items?[1]);
        Assert.Equal("otherValue", innerMember2.Value);
    }

    [Theory]
    [InlineData("not(or(status=processing,status=otherValue))")]
    public void Parse_WithNotQueryMultiple_ReturnsValidRqlOrResult(string query)
    {
        // Act
        var actualResult = _sut.Parse(query);

        // Assert
        var neq = Assert.IsType<RqlNot>(actualResult.Items![0]);
        var or = Assert.IsType<RqlOr>(neq.Nested);
        Assert.IsType<RqlEqual>(or.Items?[0]);
        Assert.IsType<RqlEqual>(or.Items?[1]);
    }

    [Theory]
    [InlineData("gt(events.created.at,2020-01-01T00:00:00+00:00)")]
    public void Parse_WithDateQuery_ReturnsValidResult(string query)
    {
        // Act
        var actualResult = _sut.Parse(query);

        // Assert
        var op = Assert.IsType<RqlGreaterThan>(actualResult.Items![0]);
        var member = Assert.IsType<RqlConstant>(op.Left);
        Assert.Equal("events.created.at", member.Value);
        var constant = Assert.IsType<RqlConstant>(op.Right);
        Assert.Equal("2020-01-01T00:00:00+00:00", constant.Value);
    }

    [Theory]
    [InlineData("like(product.name,*best*)")]
    public void Parse_WithLikeQuery_ReturnsValidResult(string query)
    {
        // Act
        var actualResult = _sut.Parse(query);

        // Assert
        var op = Assert.IsType<RqlLike>(actualResult.Items![0]);
        var member = Assert.IsType<RqlConstant>(op.Left);
        Assert.Equal("product.name", member.Value);
        var constant = Assert.IsType<RqlConstant>(op.Right);
        Assert.Equal("*best*", constant.Value);
    }

    [Theory]
    [InlineData("in(status,(processing,active,closed,created,new))")]
    public void Parse_WithArrayQuery_ReturnsValidResult(string query)
    {
        // Act
        var actualResult = _sut.Parse(query);

        // Assert
        var op = Assert.IsType<RqlListIn>(actualResult.Items![0]);
        Assert.Equal("status", Assert.IsType<RqlConstant>(op.Left).Value);
        var list = Assert.IsAssignableFrom<RqlGroup>(op.Right).Items!.OfType<RqlConstant>().ToList();
        Assert.Equal("processing", list[0].Value);
        Assert.Equal("active", list[1].Value);
        Assert.Equal("closed", list[2].Value);
        Assert.Equal("created", list[3].Value);
        Assert.Equal("new", list[4].Value);
    }

    [Theory]
    [InlineData("in(status,(active))")]
    public void Parse_WithSingleArrayQuery_ReturnsValidResult(string query)
    {
        // Act
        var actualResult = _sut.Parse(query);

        // Assert
        var op = Assert.IsType<RqlListIn>(actualResult.Items![0]);
        Assert.Equal("status", Assert.IsType<RqlConstant>(op.Left).Value);
        var list = Assert.IsAssignableFrom<RqlGroup>(op.Right).Items!.OfType<RqlConstant>().ToList();
        Assert.Equal("active", list[0].Value);
    }

    [Theory]
    [InlineData("in(status,(active,otherValue))")]
    public void Parse_WithMultipleArrayQuery_ReturnsValidResult(string query)
    {
        // Act
        var actualResult = _sut.Parse(query);

        // Assert
        var op = Assert.IsType<RqlListIn>(actualResult.Items![0]);
        Assert.Equal("status", Assert.IsType<RqlConstant>(op.Left).Value);
        var list = Assert.IsAssignableFrom<RqlGroup>(op.Right).Items!.OfType<RqlConstant>().ToList();
        Assert.Equal("active", list[0].Value);
        Assert.Equal("otherValue", list[1].Value);
    }

    [Theory]
    [InlineData("out(status,(active))")]
    [InlineData("out(status,(active,otherValue))")]
    public void Parse_WithMultipleArrayOutQuery_ReturnsValidResult(string query)
    {
        // Act
        var actualResult = _sut.Parse(query);

        // Assert
        var op = Assert.IsType<RqlListOut>(actualResult.Items![0]);
        Assert.Equal("status", Assert.IsType<RqlConstant>(op.Left).Value);
        var list = Assert.IsAssignableFrom<RqlGroup>(op.Right).Items!.OfType<RqlConstant>().ToList();
        Assert.DoesNotContain(new RqlConstant("active"), list);
        Assert.DoesNotContain(new RqlConstant("otherValue"), list);
    }

    [Theory]
    [InlineData("id=PRD-0000-0001&status=active")]
    [InlineData("and(id=PRD-0000-0001,status=active)")]
    [InlineData("id=PRD-0000-0001,status=active")]
    [InlineData("id=PRD-0000-0001,eq(status,active)")]
    [InlineData("and(eq(id,PRD-0000-0001),eq(status,active))")]
    public void Parse_WithAndQuery_ReturnsValidResult(string query)
    {
        // Act
        var actualResult = _sut.Parse(query);

        // Assert
        var and = Assert.IsType<RqlAnd>(actualResult);
        CheckListOfConstantComparisons<RqlEqual>(and.Items!, 0, "id", "PRD-0000-0001");
        CheckListOfConstantComparisons<RqlEqual>(and.Items!, 1, "status", "active");
    }

    [Theory]
    [InlineData("id=null()")]
    public void Parse_WithNullQuery_ReturnsValidResult(string query)
    {
        // Act
        var actualResult = _sut.Parse(query);

        // Assert
        var grp = Assert.IsAssignableFrom<RqlGroup>(actualResult);
        var eq = Assert.IsType<RqlEqual>(grp.Items![0]);
        Assert.IsType<RqlConstant>(eq.Left);
        Assert.IsType<RqlNull>(eq.Right);
    }

    [Theory]
    [InlineData("id=null(),name=abc")]
    [InlineData("name=abc,id=null()")]
    [InlineData("eq(id,null()),name=abc")]
    [InlineData("name=abc,eq(id,null())")]
    [InlineData("id=null()&name=abc")]
    [InlineData("name=abc&id=null()")]
    [InlineData("eq(id,null())&name=abc")]
    [InlineData("name=abc&eq(id,null())")]
    [InlineData("id=empty(),name=abc")]
    [InlineData("name=abc,id=empty()")]
    [InlineData("eq(id,empty()),name=abc")]
    [InlineData("name=abc,eq(id,empty())")]
    [InlineData("id=empty()&name=abc")]
    [InlineData("name=abc&id=empty()")]
    [InlineData("eq(id,empty())&name=abc")]
    [InlineData("name=abc&eq(id,empty())")]
    public void Parse_WithNullAndOther_OnAnd_ReturnsValidResult(string query)
    {
        // Act
        var actualResult = _sut.Parse(query);

        // Assert
        var grp = Assert.IsAssignableFrom<RqlGroup>(actualResult);
        Assert.True(grp is RqlAnd);
        var items = grp.Items!.OfType<RqlEqual>().ToList();
        Assert.Equal(2, items.Count);

        Assert.NotNull(items.FirstOrDefault(t => t.Right is RqlFunction));
        Assert.NotNull(items.FirstOrDefault(t => t.Right is RqlConstant ctn && ctn.Value == "abc"));
    }

    [Theory]
    [InlineData("id=null()|name=abc")]
    [InlineData("name=abc|id=null()")]
    [InlineData("eq(id,null())|name=abc")]
    [InlineData("name=abc|eq(id,null())")]
    [InlineData("id=null();name=abc")]
    [InlineData("name=abc;id=null()")]
    [InlineData("eq(id,null());name=abc")]
    [InlineData("name=abc;eq(id,null())")]
    [InlineData("id=empty()|name=abc")]
    [InlineData("name=abc|id=empty()")]
    [InlineData("eq(id,empty())|name=abc")]
    [InlineData("name=abc|eq(id,empty())")]
    [InlineData("id=empty();name=abc")]
    [InlineData("name=abc;id=empty()")]
    [InlineData("eq(id,empty());name=abc")]
    [InlineData("name=abc;eq(id,empty())")]
    public void Parse_WithNullAndOther_OnOr_ReturnsValidResult(string query)
    {
        // Act
        var actualResult = _sut.Parse(query);

        // Assert
        var grp = Assert.IsAssignableFrom<RqlGroup>(actualResult);
        Assert.True(grp is RqlOr);
        var items = grp.Items!.OfType<RqlEqual>().ToList();
        Assert.Equal(2, items.Count);

        Assert.NotNull(items.FirstOrDefault(t => t.Right is RqlFunction));
        Assert.NotNull(items.FirstOrDefault(t => t.Right is RqlConstant ctn && ctn.Value == "abc"));
    }

    [Theory]
    [InlineData("eq(id,1),name=abc")]
    [InlineData("name=abc,eq(id,1)")]
    public void Parse_WithParenthesisAndOther_ReturnsValidResult(string query)
    {
        // Act
        var actualResult = _sut.Parse(query);

        // Assert
        var grp = Assert.IsAssignableFrom<RqlGroup>(actualResult);
        Assert.True(grp is RqlAnd);
        var items = grp.Items!.OfType<RqlEqual>().ToList();
        Assert.Equal(2, items.Count);

        Assert.NotNull(items.FirstOrDefault(t => t.Right is RqlConstant ctn && ctn.Value == "1"));
        Assert.NotNull(items.FirstOrDefault(t => t.Right is RqlConstant ctn && ctn.Value == "abc"));
    }

    [Theory]
    [InlineData("id=empty()")]
    public void Parse_WithEmptyQuery_ReturnsValidResult(string query)
    {
        // Act
        var actualResult = _sut.Parse(query);

        // Assert
        var grp = Assert.IsAssignableFrom<RqlGroup>(actualResult);
        var eq = Assert.IsType<RqlEqual>(grp.Items![0]);
        Assert.IsType<RqlConstant>(eq.Left);
        Assert.IsType<RqlEmpty>(eq.Right);
    }

    [Theory]
    [InlineData("any(subcollection,id=1)")]
    public void Parse_WithAnyExpression_ReturnsValidResult(string query)
    {
        // Act
        var actualResult = _sut.Parse(query);

        // Assert
        var grp = Assert.IsAssignableFrom<RqlGroup>(actualResult);
        var rqlAny = Assert.IsType<RqlAny>(grp.Items![0]);
        Assert.IsType<RqlConstant>(rqlAny.Left);
        Assert.IsType<RqlEqual>(rqlAny.Right);
    }

    [Theory]
    [InlineData("all(subcollection,id=1)")]
    public void Parse_WithAllExpression_ReturnsValidResult(string query)
    {
        // Act
        var actualResult = _sut.Parse(query);

        // Assert
        var grp = Assert.IsAssignableFrom<RqlGroup>(actualResult);
        var rqlAll = Assert.IsType<RqlAll>(grp.Items![0]);
        Assert.IsType<RqlConstant>(rqlAll.Left);
        Assert.IsType<RqlEqual>(rqlAll.Right);
    }

    [Theory]
    [InlineData("id=PRD-0000-0001&status=active|mode=1&mode=2")]
    [InlineData("(id=PRD-0000-0001&status=active)|mode=1&mode=2")]
    [InlineData("id=PRD-0000-0001&status=active|(mode=1&mode=2)")]
    [InlineData("(id=PRD-0000-0001&status=active)|(mode=1&mode=2)")]
    [InlineData("((id=PRD-0000-0001&status=active)|(mode=1&mode=2))")]
    public void Parse_WithAndOrQuery_ReturnsValidResult(string query)
    {
        // Act
        var actualResult = _sut.Parse(query);

        // Assert
        var or = Assert.IsType<RqlOr>(actualResult);
        var items = or.Items!.ToList();
        var and1 = Assert.IsType<RqlAnd>(items[0]);
        CheckListOfConstantComparisons<RqlEqual>(and1.Items!, 0, "id", "PRD-0000-0001");
        CheckListOfConstantComparisons<RqlEqual>(and1.Items!, 1, "status", "active");
        var and2 = Assert.IsType<RqlAnd>(items[1]);
        CheckListOfConstantComparisons<RqlEqual>(and2.Items!, 0, "mode", "1");
        CheckListOfConstantComparisons<RqlEqual>(and2.Items!, 1, "mode", "2");
    }

    [Theory]
    [InlineData("id=PRD-0000-0001|status=active")]
    [InlineData("or(id=PRD-0000-0001,status=active)")]
    [InlineData("id=PRD-0000-0001;status=active")]
    [InlineData("or(eq(id,PRD-0000-0001),eq(status,active))")]
    public void Parse_WithOrderQuery_ReturnsValidResult(string query)
    {
        // Act
        var actualResult = _sut.Parse(query);

        // Assert
        var or = Assert.IsType<RqlOr>(actualResult);
        CheckListOfConstantComparisons<RqlEqual>(or.Items!, 0, "id", "PRD-0000-0001");
        CheckListOfConstantComparisons<RqlEqual>(or.Items!, 1, "status", "active");
    }

    [Theory]
    [InlineData("product.name='white space & special^ symbols!'")]
    [InlineData("eq(product.name,'white space & special^ symbols!')")]
    public void Parse_WithSpecialAmpersandCharacterQuery_ReturnsValidResult(string query)
    {
        // Act
        var actualResult = _sut.Parse(query);

        // Assert
        var eq = Assert.IsType<RqlEqual>(actualResult.Items![0]);
        var member = Assert.IsType<RqlConstant>(eq.Left);
        Assert.Equal("product.name", member.Value);
        var constant = Assert.IsType<RqlConstant>(eq.Right);
        Assert.Equal("white space & special^ symbols!", constant.Value);
    }

    [Theory]
    [InlineData(@"product.name='i am ""happy"" is =quoted here='")]
    [InlineData(@"eq(product.name,'i am ""happy"" is =quoted here=')")]
    public void Parse_WithSpecialQuoteCharacterQuery_ReturnsValidResult(string query)
    {
        // Act
        var actualResult = _sut.Parse(query);

        // Assert
        var eq = Assert.IsType<RqlEqual>(actualResult.Items![0]);
        var member = Assert.IsType<RqlConstant>(eq.Left);
        Assert.Equal("product.name", member.Value);
        var constant = Assert.IsType<RqlConstant>(eq.Right);
        Assert.Equal(@"i am ""happy"" is =quoted here=", constant.Value);
    }

    [Fact]
    public void Parse_WithSelectQuery_ReturnsValidResult()
    {
        // Act
        var actualResult = _sut.Parse("id,sub(*,-id),-id");

        // Assert
        var grp = Assert.IsAssignableFrom<RqlGroup>(actualResult);
        var item1 = Assert.IsType<RqlConstant>(grp.Items![0]);
        Assert.Equal("id", item1.Value);
        var item2 = Assert.IsType<RqlGenericGroup>(grp.Items[1]);
        Assert.Equal("sub", item2.Name);
        Assert.Equal("*", Assert.IsType<RqlConstant>(item2.Items![0]).Value);
        Assert.Equal("-id", Assert.IsType<RqlConstant>(item2.Items[1]).Value);
        Assert.Equal("-id", Assert.IsType<RqlConstant>(grp.Items[2]).Value);
    }

    [Fact]
    public void Parse_WithSelectMinusWildcard_ReturnsValidResult()
    {
        // Act
        var actualResult = _sut.Parse("id,-*");

        // Assert
        var grp = Assert.IsAssignableFrom<RqlGroup>(actualResult);
        var item1 = Assert.IsType<RqlConstant>(grp.Items![0]);
        Assert.Equal("id", item1.Value);
        var item2 = Assert.IsType<RqlConstant>(grp.Items[1]);
        Assert.Equal("-*", item2.Value);
    }

    [Theory]
    [InlineData("(ilike(number,'*HL*')|ilike(name,'*HL*'))&order=id&offset=0&limit=30")]
    public void Parse_WithILikeEscapeSpecialCharacters_ReturnsValidResult(string query)
    {
        // Act
        var actualResult = _sut.Parse(query);

        // Assert
        var andExpression = Assert.IsType<RqlAnd>(actualResult);

        var orILikeExpression = Assert.IsType<RqlOr>(andExpression.Items?[0]);
        var iLikeExpression1 = Assert.IsType<RqlLikeCaseInsensitive>(orILikeExpression.Items?[0]);
        var iLikeExpression2 = Assert.IsType<RqlLikeCaseInsensitive>(orILikeExpression.Items?[1]);

        Assert.Equal("*HL*", ((RqlConstant)iLikeExpression1.Right).Value);
        Assert.Equal("*HL*", ((RqlConstant)iLikeExpression2.Right).Value);
        Assert.IsType<RqlEqual>(andExpression.Items?[1]);
        Assert.IsType<RqlEqual>(andExpression.Items?[2]);
        Assert.IsType<RqlEqual>(andExpression.Items?[3]);
    }

    [Fact]
    public void Parse_WithParameterlessSelfPointer_ReturnsValidResult()
    {
        // Act
        var actualResult = _sut.Parse("eq(self(),3)");

        // Assert
        var eq = Assert.IsType<RqlEqual>(actualResult.Items![0]);
        var pointer = Assert.IsType<RqlSelf>(eq.Left);
        Assert.Null(pointer.Inner);
        var constant = Assert.IsType<RqlConstant>(eq.Right);
        Assert.Equal("3", constant.Value);
    }


    [Fact]
    public void Parse_WithParameterizedSelfPointer_ReturnsValidResult()
    {
        // Act
        var actualResult = _sut.Parse("eq(self(id),3)");

        // Assert
        var eq = Assert.IsType<RqlEqual>(actualResult.Items![0]);
        var pointer = Assert.IsType<RqlSelf>(eq.Left);
        var inner = Assert.IsType<RqlConstant>(pointer.Inner);
        Assert.Equal("id", inner.Value);
        var constant = Assert.IsType<RqlConstant>(eq.Right);
        Assert.Equal("3", constant.Value);
    }

    [Fact]
    public void Parse_WithDoubleParameterizedSelfPointer_ReturnsValidResult()
    {
        // Act
        var actualResult = _sut.Parse("eq(self(self(id)),3)");

        // Assert
        var eq = Assert.IsType<RqlEqual>(actualResult.Items![0]);
        var pointer1 = Assert.IsType<RqlSelf>(eq.Left);
        var pointer2 = Assert.IsType<RqlSelf>(pointer1.Inner);
        var inner = Assert.IsType<RqlConstant>(pointer2.Inner);
        Assert.Equal("id", inner.Value);
        var constant = Assert.IsType<RqlConstant>(eq.Right);
        Assert.Equal("3", constant.Value);
    }

    private static void CheckListOfConstantComparisons<TComparison>(IReadOnlyList<RqlExpression> items, int index, string prop, string value)
        where TComparison : RqlBinary
    {
        var item = Assert.IsType<TComparison>(items[index]);
        var itemMember = Assert.IsType<RqlConstant>(item.Left);
        var itemConst = Assert.IsType<RqlConstant>(item.Right);
        Assert.Equal(prop, itemMember.Value);
        Assert.Equal(value, itemConst.Value);
    }
}