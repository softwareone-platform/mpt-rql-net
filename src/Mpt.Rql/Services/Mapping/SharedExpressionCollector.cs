using System.Linq.Expressions;

namespace Mpt.Rql.Services.Mapping;

internal class SharedExpressionCollector
{
    private readonly Dictionary<string, LiftedEntry> _entries = new();

    public bool HasEntries => _entries.Count > 0;

    public IReadOnlyCollection<LiftedEntry> Entries => _entries.Values;

    public ParameterExpression LiftExpression(Expression original, string key)
    {
        if (_entries.TryGetValue(key, out var existing))
            return existing.Placeholder;

        var placeholder = Expression.Parameter(original.Type, $"__sub{_entries.Count}");
        _entries[key] = new LiftedEntry(original, placeholder, key);
        return placeholder;
    }
}

internal readonly struct LiftedEntry(Expression originalExpression, ParameterExpression placeholder, string key)
{
    public readonly Expression OriginalExpression = originalExpression;
    public readonly ParameterExpression Placeholder = placeholder;
    public readonly string Key = key;
}
