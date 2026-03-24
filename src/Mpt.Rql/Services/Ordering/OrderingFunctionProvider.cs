using System.Diagnostics.CodeAnalysis;

namespace Mpt.Rql.Services.Ordering;

/// <summary>
/// Default <see cref="IOrderingFunctionProvider"/> that resolves functions from all
/// <see cref="IOrderingFunction"/> registrations in the DI container.
/// </summary>
/// <remarks>
/// Functions are keyed by <see cref="IOrderingFunction.FunctionName"/> and looked up
/// case-insensitively, so <c>OrderBy</c> and <c>orderby</c> resolve to the same function.
/// </remarks>
internal class OrderingFunctionProvider : IOrderingFunctionProvider
{
    private readonly Dictionary<string, IOrderingFunction> _functions;

    /// <param name="functions">All <see cref="IOrderingFunction"/> instances registered with DI.</param>
    public OrderingFunctionProvider(IEnumerable<IOrderingFunction> functions)
    {
        _functions = functions.ToDictionary(f => f.FunctionName, StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc/>
    public bool TryGet(string functionName, [NotNullWhen(true)] out IOrderingFunction? function)
        => _functions.TryGetValue(functionName, out function);
}
