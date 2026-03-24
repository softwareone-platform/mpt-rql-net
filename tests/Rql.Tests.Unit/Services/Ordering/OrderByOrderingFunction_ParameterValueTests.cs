using FluentAssertions;
using Moq;
using Mpt.Rql.Abstractions.Configuration;
using Mpt.Rql.Core;
using Mpt.Rql.Core.Metadata;
using Mpt.Rql.Services.Context;
using Mpt.Rql.Services.Ordering;
using Mpt.Rql.Services.Ordering.Functions;
using Mpt.Rql.Settings;
using Rql.Tests.Unit.Services.Models;
using System.Linq.Expressions;
using Xunit;

namespace Rql.Tests.Unit.Services.Ordering;

/// <summary>
/// Component tests for <see cref="OrderByOrderingFunction"/> using a <c>Name</c>/<c>Value</c>
/// parameter collection — the primary real-world use case:
///
/// <code>
///   +orderby(parameters, name, priority, value)
/// </code>
///
/// Finds the first Parameter whose Name == "priority" and uses its Value (a nullable string)
/// as the sort key.
/// </summary>
public class OrderByOrderingFunction_ParameterValueTests
{
    private static OrderByOrderingFunction MakeFunction()
    {
        var actionValidator = new Mock<IActionValidator>();
        actionValidator.Setup(a => a.Validate(It.IsAny<RqlPropertyInfo>(), It.IsAny<Mpt.Rql.RqlActions>())).Returns(true);

        var settings = new RqlSettings();
        var metadataProvider = new MetadataProvider(new PropertyNameProvider(), new MetadataFactory(new GlobalRqlSettings()));
        var builderContext = new BuilderContext();
        var pathBuilder = new OrderingPathInfoBuilder(actionValidator.Object, metadataProvider, builderContext, settings);

        return new OrderByOrderingFunction(pathBuilder);
    }

    // ── Return type ──────────────────────────────────────────────────────────

    [Fact]
    public void Build_StringValueResult_ReturnsStringExpression()
    {
        // Value is already a nullable string — no extra wrapping needed
        var func = MakeFunction();
        var param = Expression.Parameter(typeof(SupportCase), "sc");

        var result = func.Build(param, ["parameters", "name", "priority", "value"]);

        result.IsError.Should().BeFalse();
        result.Value!.Type.Should().Be(typeof(string));
    }

    // ── Correct value extraction ─────────────────────────────────────────────

    [Fact]
    public void Build_MatchingParameter_ReturnsValue()
    {
        var func = MakeFunction();
        var param = Expression.Parameter(typeof(SupportCase), "sc");
        var built = func.Build(param, ["parameters", "name", "priority", "value"]);
        built.IsError.Should().BeFalse();

        var lambda = Expression.Lambda<Func<SupportCase, string?>>(built.Value!, param).Compile();
        var sc = new SupportCase
        {
            Id = 1,
            Title = "T",
            Parameters =
            [
                new Parameter { Name = "status",   Value = "open"     },
                new Parameter { Name = "priority",  Value = "critical" },  // match
                new Parameter { Name = "category",  Value = "billing"  },
            ]
        };

        lambda(sc).Should().Be("critical");
    }

    [Fact]
    public void Build_UsesFirstMatch_WhenMultipleParametersShareSameName()
    {
        // If two parameters both have Name="priority", the first one wins
        var func = MakeFunction();
        var param = Expression.Parameter(typeof(SupportCase), "sc");
        var built = func.Build(param, ["parameters", "name", "priority", "value"]);
        built.IsError.Should().BeFalse();

        var lambda = Expression.Lambda<Func<SupportCase, string?>>(built.Value!, param).Compile();
        var sc = new SupportCase
        {
            Id = 1,
            Title = "T",
            Parameters =
            [
                new Parameter { Name = "priority", Value = "high"     },   // first → wins
                new Parameter { Name = "priority", Value = "critical" },   // second → ignored
            ]
        };

        lambda(sc).Should().Be("high");
    }

    [Fact]
    public void Build_ParameterWithNullValue_ReturnsNull()
    {
        // The matching parameter exists but its Value is null
        var func = MakeFunction();
        var param = Expression.Parameter(typeof(SupportCase), "sc");
        var built = func.Build(param, ["parameters", "name", "priority", "value"]);
        built.IsError.Should().BeFalse();

        var lambda = Expression.Lambda<Func<SupportCase, string?>>(built.Value!, param).Compile();
        var sc = new SupportCase
        {
            Id = 1,
            Title = "T",
            Parameters = [new Parameter { Name = "priority", Value = null }]
        };

        lambda(sc).Should().BeNull();
    }

    // ── Null / empty collection ──────────────────────────────────────────────

    [Fact]
    public void Build_NoMatchingParameter_ReturnsNull()
    {
        var func = MakeFunction();
        var param = Expression.Parameter(typeof(SupportCase), "sc");
        var built = func.Build(param, ["parameters", "name", "priority", "value"]);
        built.IsError.Should().BeFalse();

        var lambda = Expression.Lambda<Func<SupportCase, string?>>(built.Value!, param).Compile();
        var sc = new SupportCase
        {
            Id = 1,
            Title = "T",
            Parameters = [new Parameter { Name = "status", Value = "open" }]  // no "priority"
        };

        lambda(sc).Should().BeNull();
    }

    [Fact]
    public void Build_EmptyParameterCollection_ReturnsNull()
    {
        var func = MakeFunction();
        var param = Expression.Parameter(typeof(SupportCase), "sc");
        var built = func.Build(param, ["parameters", "name", "priority", "value"]);
        built.IsError.Should().BeFalse();

        var lambda = Expression.Lambda<Func<SupportCase, string?>>(built.Value!, param).Compile();
        var sc = new SupportCase { Id = 1, Title = "T", Parameters = [] };

        lambda(sc).Should().BeNull();
    }

    // ── Filter is case-sensitive ─────────────────────────────────────────────

    [Fact]
    public void Build_FilterIsCaseSensitive_WrongCaseYieldsNull()
    {
        // The == operator in the generated expression is ordinal (default for strings in C#)
        var func = MakeFunction();
        var param = Expression.Parameter(typeof(SupportCase), "sc");
        var built = func.Build(param, ["parameters", "name", "priority", "value"]);
        built.IsError.Should().BeFalse();

        var lambda = Expression.Lambda<Func<SupportCase, string?>>(built.Value!, param).Compile();
        var sc = new SupportCase
        {
            Id = 1,
            Title = "T",
            Parameters = [new Parameter { Name = "Priority", Value = "high" }]  // capital P — no match
        };

        lambda(sc).Should().BeNull();
    }
}
