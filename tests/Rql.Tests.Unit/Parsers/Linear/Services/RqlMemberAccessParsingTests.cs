using FluentAssertions;
using Mpt.Rql.Abstractions;
using Mpt.Rql.Abstractions.Argument;
using Mpt.Rql.Abstractions.Argument.Pointer;
using Mpt.Rql.Abstractions.Binary;
using Mpt.Rql.Abstractions.Group;
using Mpt.Rql.Parsers.Linear.Services;
using Xunit;

namespace Rql.Tests.Unit.Parsers.Linear.Services;

/// <summary>A dotted path directly after a closing parenthesis is a member access on the call's result.</summary>
public class RqlMemberAccessParsingTests
{
    private readonly RqlParser _sut = new();

    [Fact]
    public void Parse_CallFollowedByPath_ReturnsMemberAccessOverTheGroup()
    {
        var result = _sut.Parse("+first(parameters,eq(name,\"priority\")).value");

        var member = result.Should().BeOfType<RqlGenericGroup>().Subject.Items.Should().ContainSingle().Subject
            .Should().BeOfType<RqlMemberAccess>().Subject;
        member.Path.Should().Be("value");
        var group = member.Inner.Should().BeOfType<RqlGenericGroup>().Subject;
        group.Name.Should().Be("+first");
        group.Items.Should().HaveCount(2);
        group.Items![0].Should().BeOfType<RqlConstant>().Which.Value.Should().Be("parameters");
        var predicate = group.Items[1].Should().BeOfType<RqlEqual>().Subject;
        predicate.Right.Should().BeOfType<RqlConstant>().Which.Should().BeEquivalentTo(new { Value = "priority", IsQuoted = true });
    }

    [Fact]
    public void Parse_DottedPathAfterCall_KeepsTheWholePath()
    {
        var result = _sut.Parse("first(orders).customer.name");

        var member = result.Should().BeOfType<RqlGenericGroup>().Subject.Items![0].Should().BeOfType<RqlMemberAccess>().Subject;
        member.Path.Should().Be("customer.name");
        member.Inner.Should().BeOfType<RqlGenericGroup>().Which.Items.Should().HaveCount(1);
    }

    [Fact]
    public void Parse_CallFollowedByPath_ThenAnotherTerm_ProducesTwoItems()
    {
        var result = _sut.Parse("-first(parameters,eq(name,priority)).value,+id");

        var and = result.Should().BeOfType<RqlAnd>().Subject;
        and.Items.Should().HaveCount(2);
        and.Items![0].Should().BeOfType<RqlMemberAccess>().Which.Path.Should().Be("value");
        and.Items[1].Should().BeOfType<RqlConstant>().Which.Value.Should().Be("+id");
    }


    [Theory]
    [InlineData("name=first(orders).id", "id")]
    [InlineData("name=self(other).id", "id")]
    public void Parse_MemberAccessAsEqualsShortcutRightSide_IsTheRightOperand(string query, string path)
    {
        // The shortcut's right side ends on the path, not on ')'; the parser must resume after it and never throw.
        var result = _sut.Parse(query);

        var eq = result.Should().BeOfType<RqlGenericGroup>().Subject.Items![0].Should().BeOfType<RqlEqual>().Subject;
        eq.Left.Should().BeOfType<RqlConstant>().Which.Value.Should().Be("name");
        eq.Right.Should().BeOfType<RqlMemberAccess>().Which.Path.Should().Be(path);
    }

    [Theory]
    [InlineData("name=first(orders).id,eq(id,1)")]
    [InlineData("name=self(other),eq(id,1)")]
    [InlineData("and(name=first(orders).id,eq(id,1))")]
    public void Parse_ParenthesisedEqualsShortcutRightSide_KeepsTheFollowingTerm(string query)
    {
        var result = _sut.Parse(query);

        var items = result.Should().BeAssignableTo<RqlGroup>().Subject.Items!;
        items.Should().HaveCount(2);
        items[0].Should().BeOfType<RqlEqual>().Which.Right.Should().BeAssignableTo<RqlPointer>();
        var second = items[1].Should().BeOfType<RqlEqual>().Subject;
        second.Left.Should().BeOfType<RqlConstant>().Which.Value.Should().Be("id");
        second.Right.Should().BeOfType<RqlConstant>().Which.Value.Should().Be("1");
    }


    [Theory]
    [InlineData("name=self(other)x")]
    [InlineData("name=first(orders).id(x)")]
    public void Parse_GarbageAfterParenthesisedEqualsShortcutRightSide_IsParsedAsTheNextItem(string query)
    {
        // Whatever follows the call must start where the loop resumes; a misaligned word start used to overrun the query.
        var result = _sut.Parse(query);

        var items = result.Should().BeAssignableTo<RqlGroup>().Subject.Items!;
        items.Should().HaveCount(2);
        items[0].Should().BeOfType<RqlEqual>().Which.Right.Should().BeAssignableTo<RqlPointer>();
        items[1].Should().NotBeOfType<RqlEqual>();
    }

    [Fact]
    public void Parse_CallWithoutPath_IsStillAPlainGroup()
    {
        var result = _sut.Parse("first(parameters,eq(name,priority))");

        result.Should().BeOfType<RqlGenericGroup>().Which.Name.Should().Be("first");
    }

    [Fact]
    public void Parse_MemberAccessInsideBinary_IsTheLeftOperand()
    {
        var result = _sut.Parse("eq(first(parameters,eq(name,priority)).value,high)");

        var eq = result.Should().BeOfType<RqlGenericGroup>().Subject.Items![0].Should().BeOfType<RqlEqual>().Subject;
        eq.Left.Should().BeOfType<RqlMemberAccess>().Which.Path.Should().Be("value");
        eq.Right.Should().BeOfType<RqlConstant>().Which.Value.Should().Be("high");
    }

    [Fact]
    public void Parse_TrailingDotWithoutPath_IsIgnored()
    {
        var result = _sut.Parse("first(parameters).");

        result.Should().BeOfType<RqlGenericGroup>().Which.Name.Should().Be("first");
    }
}
