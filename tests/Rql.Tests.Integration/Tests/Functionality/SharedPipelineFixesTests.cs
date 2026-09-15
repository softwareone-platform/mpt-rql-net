using Mpt.Rql;
using Rql.Tests.Integration.Core;
using System.Collections.Immutable;
using Xunit;

namespace Rql.Tests.Integration.Tests.Functionality;

/// <summary>
/// Behaviour changes to shared filtering/parsing infrastructure that came with <c>first()</c> and apply to
/// plain <c>filter=</c>, <c>order=</c> and <c>select=</c> requests as well.
/// </summary>
public class SharedPipelineFixesTests
{
    private static IRqlQueryable<T, T> Make<T>() =>
        RqlFactory.Make<T>(services => { }, rql =>
        {
            rql.Settings.Mapping.Transparent = true;
            rql.Settings.Select.Implicit = RqlSelectModes.Core | RqlSelectModes.Primitive;
            rql.Settings.Select.Explicit = RqlSelectModes.All;
        });

    // ── Malformed input is a validation error in every parameter, never an exception ──

    [Theory]
    [InlineData("eq(name)", null, null, "query:malformed")]
    [InlineData(null, "+first(orders,id,eq(clientName))", null, "order:malformed")]
    [InlineData(null, "eq(name)", null, "order:malformed")]
    [InlineData(null, null, "eq(name)", "select:malformed")]
    public void MalformedExpression_IsAValidationError(string? filter, string? order, string? select, string expectedCode)
    {
        var result = Make<Product>().Transform(ProductRepository.Query(), new RqlRequest { Filter = filter, Order = order, Select = select });

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Code == expectedCode && e.Message.StartsWith("Malformed "));
    }

    // ── Unquoted right-hand constants: property when compatible, literal otherwise ──

    [Fact]
    public void RightHandProperty_IncompatibleType_FallsBackToLiteral_StringSide()
    {
        // `id` is an int property; it cannot be compared to the string `name`, so it is the literal "id".
        var result = Make<Product>().Transform(ProductRepository.Query(), new RqlRequest { Filter = "eq(name,id)" });

        Assert.True(result.IsSuccess, string.Join("; ", result.Errors));
        Assert.Empty(result.Query.ToList());
    }

    [Fact]
    public void RightHandProperty_IncompatibleType_FallsBackToLiteral_IntSide()
    {
        // `name` is a string property; it cannot be compared to the int `id`, so it is the literal "name" — which is not an int.
        var result = Make<Product>().Transform(ProductRepository.Query(), new RqlRequest { Filter = "eq(id,name)" });

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message.Contains("Cannot convert value: 'name'"));
    }

    [Fact]
    public void RightHandProperty_CompatibleType_IsComparedAsProperty()
    {
        // price == sellPrice: product 6 (129.99 both)
        var result = Make<Product>().Transform(ProductRepository.Query(), new RqlRequest { Filter = "eq(price,sellPrice)" });

        Assert.True(result.IsSuccess, string.Join("; ", result.Errors));
        Assert.Equal([6], result.Query.Select(p => p.Id).ToList());
    }

    // ── Struct enumerables are rejected as validation errors in any() too ──

    [Fact]
    public void Any_OnStructEnumerable_IsAValidationError()
    {
        var data = new List<FrozenCase> { new() { Id = 1, Parameters = [new CaseParameter { Name = "x" }] } }.AsQueryable();

        var result = Make<FrozenCase>().Transform(data, new RqlRequest { Filter = "any(parameters,eq(name,x))" });

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Code == "rql_validation" && e.Message == "Collection property has incompatible type" && e.Path == "parameters");
    }

    // ── Nested any() keeps the outer collection prefix in error paths ──

    [Fact]
    public void NestedAny_ErrorAfterInnerAny_KeepsOuterPrefix()
    {
        var result = Make<Product>().Transform(ProductRepository.Query(), new RqlRequest { Filter = "any(collection,and(any(orders,eq(clientName,x)),eq(nonExistent,1)))" });

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message == "Invalid property path." && e.Path == "collection.nonExistent");
    }
}
