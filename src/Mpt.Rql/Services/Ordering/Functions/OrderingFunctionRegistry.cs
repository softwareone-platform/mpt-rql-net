using System.Diagnostics.CodeAnalysis;

namespace Mpt.Rql.Services.Ordering.Functions;

/// <summary>
/// Resolves <see cref="IOrderingFunction"/>s by name, case-insensitively. When several registrations
/// share a name the last one wins, so a duplicate <c>AddRql()</c> call cannot break ordering.
/// </summary>
internal sealed class OrderingFunctionRegistry
{
    private readonly Dictionary<string, IOrderingFunction> _functions = new(StringComparer.OrdinalIgnoreCase);

    public OrderingFunctionRegistry(IEnumerable<IOrderingFunction> functions)
    {
        foreach (var function in functions)
            _functions[function.Name] = function;
    }

    public bool TryGet(string name, [NotNullWhen(true)] out IOrderingFunction? function)
        => _functions.TryGetValue(name, out function);

    public bool Contains(string name) => _functions.ContainsKey(name);
}
