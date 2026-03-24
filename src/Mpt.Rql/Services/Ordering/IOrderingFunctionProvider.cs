using System.Diagnostics.CodeAnalysis;

namespace Mpt.Rql.Services.Ordering;

/// <summary>
/// Resolves <see cref="IOrderingFunction"/> implementations by their declared function name.
/// </summary>
internal interface IOrderingFunctionProvider
{
    /// <summary>
    /// Looks up a registered ordering function by name (case-insensitive).
    /// </summary>
    /// <param name="functionName">The name as it appears in the RQL sort expression.</param>
    /// <param name="function">The resolved function, or <c>null</c> when not found.</param>
    /// <returns><c>true</c> if a function with the given name is registered; otherwise <c>false</c>.</returns>
    bool TryGet(string functionName, [NotNullWhen(true)] out IOrderingFunction? function);
}
