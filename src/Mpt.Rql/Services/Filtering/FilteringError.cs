using Mpt.Rql.Abstractions.Result;

namespace Mpt.Rql.Services.Filtering;

internal static class FilteringError
{
    public static Error Internal { get; } = Error.General("Internal filtering error occurred. Please contact RQL package maintainer.", "internal");

    public static Error EmptyGroup { get; } = Error.Validation("Expression group cannot be empty.");

    public static Error NotAnExpression(string? token = null) => Error.Validation("Expression expected.", path: token);

    /// <summary>An explicit property-to-property comparison (e.g. <c>eq(id,self(name))</c>) whose sides cannot be coerced to a common type.</summary>
    public static Error IncompatibleComparison(Type left, Type right)
        => Error.Validation($"Cannot compare properties of incompatible types '{left.Name}' and '{right.Name}'.", "query:incompatible_types");
}
