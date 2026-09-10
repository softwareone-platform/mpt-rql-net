using FluentAssertions;
using Mpt.Rql.Core;
using Mpt.Rql.Services.Ordering.Functions;
using System.Linq.Expressions;
using Xunit;

namespace Rql.Tests.Unit.Ordering;

public class OrderingFunctionRegistryTests
{
    private sealed class StubFunction(string name) : IOrderingFunction
    {
        public string Name => name;
        public Result<Expression> Build(OrderingFunctionContext context) => throw new NotSupportedException();
    }

    [Fact]
    public void TryGet_IsCaseInsensitive()
    {
        var first = new StubFunction("first");
        var registry = new OrderingFunctionRegistry([first]);

        registry.TryGet("FIRST", out var resolved).Should().BeTrue();
        resolved.Should().BeSameAs(first);
        registry.Contains("First").Should().BeTrue();
    }

    [Fact]
    public void DuplicateName_LastRegistrationWins_AndDoesNotThrow()
    {
        var older = new StubFunction("first");
        var newer = new StubFunction("first");

        var registry = new OrderingFunctionRegistry([older, newer]);

        registry.TryGet("first", out var resolved).Should().BeTrue();
        resolved.Should().BeSameAs(newer);
    }

    [Fact]
    public void UnknownName_IsNotFound()
    {
        var registry = new OrderingFunctionRegistry([new StubFunction("first")]);

        registry.TryGet("nope", out var resolved).Should().BeFalse();
        resolved.Should().BeNull();
        registry.Contains("nope").Should().BeFalse();
    }
}
