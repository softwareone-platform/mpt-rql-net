using FluentAssertions;
using Mpt.Rql;
using Mpt.Rql.Abstractions.Exception;
using Rql.Tests.Integration.Core;
using Xunit;

namespace Rql.Tests.Integration.Tests.Functionality;

/// <summary>
/// End to end behaviour of <see cref="RqlPropertyAttribute.DependsOn"/> through a mapped storage to view transform,
/// where the computed <c>Value</c> of a view parameter reads <c>RawValue</c> and <c>Format</c>.
/// </summary>
public class DependsOnSelectTests
{
    // implicit Core | Primitive and explicit All,
    // so a request selecting parameters asks for every primitive of the parameter view
    private static IRqlQueryable<DependsOnOrder, DependsOnOrderView> MakeRql() =>
        RqlFactory.Make<DependsOnOrder, DependsOnOrderView>(services => { }, rql =>
        {
            rql.Settings.Mapping.Transparent = false;
            rql.Settings.Select.Implicit = RqlSelectModes.Core | RqlSelectModes.Primitive;
            rql.Settings.Select.Explicit = RqlSelectModes.All;
        });

    /// <summary>
    /// Order 1: p1 "abc" upper (value "ABC"), p2 "def" plain (value "def").
    /// Order 2: p3 "Ghi" with no format (value "Ghi").
    /// </summary>
    private static IQueryable<DependsOnOrder> Data() => new List<DependsOnOrder>
    {
        new()
        {
            Id = 1,
            Parameters =
            [
                new() { ExternalId = "p1", RawValue = "abc", Format = "upper" },
                new() { ExternalId = "p2", RawValue = "def", Format = "plain" },
            ],
        },
        new()
        {
            Id = 2,
            Parameters = [new() { ExternalId = "p3", RawValue = "Ghi", Format = null }],
        },
    }.AsQueryable();

    [Fact]
    public void Select_ComputedValueOnly_ComputesValueFromProjectedDependencies()
    {
        // Arrange
        var rql = MakeRql();

        // Act
        var result = rql.Transform(Data(), new RqlRequest { Select = "-*,id,parameters.value" });

        // Assert
        result.IsSuccess.Should().BeTrue(string.Join("; ", result.Errors.Select(e => e.Message)));
        var orders = result.Query.ToList();

        // expected values come from the getter applied to the seeded RawValue and Format
        orders.Single(o => o.Id == 1).Parameters.Select(p => p.Value).Should().Equal("ABC", "def");
        orders.Single(o => o.Id == 2).Parameters.Select(p => p.Value).Should().Equal("Ghi");

        // externalId is neither requested nor a dependency
        orders.SelectMany(o => o.Parameters).Should().OnlyContain(p => p.ExternalId == null);
    }

    [Fact]
    public void Select_ComputedValueOnly_DependenciesCarryOnlyTheDependencyIncludeReason()
    {
        // Arrange
        var rql = MakeRql();

        // Act
        var result = rql.Transform(Data(), new RqlRequest { Select = "-*,id,parameters.value" });

        // Assert
        result.IsSuccess.Should().BeTrue(string.Join("; ", result.Errors.Select(e => e.Message)));
        result.Graph.TryGetChild("parameters", out var parameters).Should().BeTrue();
        parameters!.TryGetChild("rawValue", out var rawValue).Should().BeTrue();
        parameters.TryGetChild("format", out var format).Should().BeTrue();

        rawValue!.IncludeReason.Should().Be(IncludeReasons.Dependency);
        rawValue.IsIncluded.Should().BeTrue();
        format!.IncludeReason.Should().Be(IncludeReasons.Dependency);
        format.IsIncluded.Should().BeTrue();
    }

    [Fact]
    public void Select_SiblingOfComputedValueOnly_LeavesDependenciesUnboundAndValueNull()
    {
        // Arrange
        var rql = MakeRql();

        // Act
        var result = rql.Transform(Data(), new RqlRequest { Select = "-*,id,parameters.externalId" });

        // Assert
        result.IsSuccess.Should().BeTrue(string.Join("; ", result.Errors.Select(e => e.Message)));
        var parameters = result.Query.ToList().SelectMany(o => o.Parameters).ToList();

        parameters.Select(p => p.ExternalId).Should().Equal("p1", "p2", "p3");
        parameters.Should().OnlyContain(p => p.RawValue == null && p.Format == null && p.Value == null);
    }

    [Fact]
    public void Select_WholeCollection_ComputesValue()
    {
        // Arrange
        var rql = MakeRql();

        // Act
        var result = rql.Transform(Data(), new RqlRequest { Select = "-*,id,parameters" });

        // Assert
        result.IsSuccess.Should().BeTrue(string.Join("; ", result.Errors.Select(e => e.Message)));
        var orders = result.Query.ToList();

        // expected values come from the getter applied to the seeded RawValue and Format
        orders.Single(o => o.Id == 1).Parameters.Select(p => p.Value).Should().Equal("ABC", "def");
        orders.Single(o => o.Id == 2).Parameters.Select(p => p.Value).Should().Equal("Ghi");
        orders.SelectMany(o => o.Parameters).Select(p => p.ExternalId).Should().Equal("p1", "p2", "p3");
    }

    [Fact]
    public void Select_ComputedValueWithNonSelectableDependencyDeselected_StillBindsDependency()
    {
        // Arrange
        var rql = MakeRql();

        // Act
        var result = rql.Transform(Data(), new RqlRequest { Select = "-*,id,parameters.value,-parameters.format" });

        // Assert
        result.IsSuccess.Should().BeTrue(string.Join("; ", result.Errors.Select(e => e.Message)));
        var orders = result.Query.ToList();

        // "ABC" can only come from Format "upper" being bound on p1
        orders.Single(o => o.Id == 1).Parameters.Select(p => p.Value).Should().Equal("ABC", "def");
        orders.Single(o => o.Id == 1).Parameters.Select(p => p.Format).Should().Equal("upper", "plain");
    }

    [Fact]
    public void Select_DependencyDirectly_LeavesItOut()
    {
        // Arrange
        var rql = MakeRql();

        // Act
        var result = rql.Transform(Data(), new RqlRequest { Select = "-*,id,parameters.rawValue" });

        // Assert
        result.IsSuccess.Should().BeTrue(string.Join("; ", result.Errors.Select(e => e.Message)));
        var parameters = result.Query.ToList().SelectMany(o => o.Parameters).ToList();

        // three seeded parameters, none of whose fields is projected
        parameters.Should().HaveCount(3);
        parameters.Should().OnlyContain(p => p.RawValue == null && p.Format == null && p.Value == null && p.ExternalId == null);
    }

    [Fact]
    public void Filter_OnComputedValue_IsRejected()
    {
        // Arrange
        var rql = MakeRql();

        // Act
        var result = rql.Transform(Data(), new RqlRequest { Filter = "any(parameters,eq(value,ABC))" });

        // Assert
        result.IsSuccess.Should().BeFalse();
        var error = result.Errors.Should().ContainSingle(string.Join("; ", result.Errors.Select(e => $"{e.Path}: {e.Message}"))).Subject;
        error.Message.Should().Be("Filtering is not permitted.");
        error.Path.Should().Be("parameters.value");
    }

    [Fact]
    public void Filter_OnDependency_IsRejected()
    {
        // Arrange
        var rql = MakeRql();

        // Act
        var result = rql.Transform(Data(), new RqlRequest { Filter = "any(parameters,eq(rawValue,abc))" });

        // Assert
        result.IsSuccess.Should().BeFalse();
        var error = result.Errors.Should().ContainSingle(string.Join("; ", result.Errors.Select(e => $"{e.Path}: {e.Message}"))).Subject;
        error.Message.Should().Be("Filtering is not permitted.");
        error.Path.Should().Be("parameters.rawValue");
    }

    [Fact]
    public void Transform_ViewDependingOnUnknownProperty_ThrowsInvalidDependency()
    {
        // Arrange
        var rql = RqlFactory.Make<DependsOnOrder, DependsOnMisconfiguredView>(services => { });

        // Act
        var act = () => rql.Transform(Data(), new RqlRequest { Select = "id" });

        // Assert
        act.Should().Throw<RqlInvalidDependencyException>()
            .WithMessage("*DependsOnMisconfiguredView).Label depends on 'Missing', which is not a property of its type.");
    }
}

// storage models
internal class DependsOnOrder
{
    public int Id { get; set; }

    public List<DependsOnOrderParameter> Parameters { get; set; } = [];
}

internal class DependsOnOrderParameter
{
    public string? ExternalId { get; set; }

    public string? RawValue { get; set; }

    public string? Format { get; set; }
}

// view models, mapped from storage by name with no mapper
internal class DependsOnOrderView
{
    [RqlProperty(IsCore = true)]
    public int Id { get; set; }

    public List<DependsOnParameterView> Parameters { get; set; } = null!;
}

internal class DependsOnParameterView
{
    public string? ExternalId { get; set; }

    [RqlProperty(Actions = RqlActions.None)]
    public string? RawValue { get; set; }

    [RqlProperty(Actions = RqlActions.None)]
    public string? Format { get; set; }

    [RqlProperty(Actions = RqlActions.Select, DependsOn = new[] { nameof(RawValue), nameof(Format) })]
    public string? Value => Format == "upper" ? RawValue?.ToUpperInvariant() : RawValue;
}

// used only by the misconfiguration test, and deliberately without a mapper anywhere in the assembly
internal class DependsOnMisconfiguredView
{
    public int Id { get; set; }

    [RqlProperty(Actions = RqlActions.Select, DependsOn = new[] { "Missing" })]
    public string Label => Id.ToString();
}
