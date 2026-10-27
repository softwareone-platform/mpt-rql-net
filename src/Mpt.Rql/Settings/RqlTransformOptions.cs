using System.Linq.Expressions;
using Mpt.Rql.Abstractions.Configuration;
using Mpt.Rql.Core;

namespace Mpt.Rql.Settings;

internal sealed class RqlTransformOptions(IRqlSettings settings) : IRqlTransformOptions
{
    private Dictionary<string, RqlVisibility>? _decisions;
    private List<RqlCollectionScope>? _collectionScopes;
    public IRqlSettings Settings { get; } = settings;

    /// <summary>The visibility set on each path, or <c>null</c> when none was set.</summary>
    public IReadOnlyDictionary<string, RqlVisibility>? Decisions => _decisions;

    /// <summary>The collection restrictions registered for this transformation.</summary>
    public IReadOnlyList<RqlCollectionScope> CollectionScopes =>
        _collectionScopes is null
            ? Array.Empty<RqlCollectionScope>()
            : _collectionScopes;

    public void SetVisibility(string path, RqlVisibility visibility)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        if (!Enum.IsDefined(visibility))
            throw new ArgumentOutOfRangeException(nameof(visibility));

        (_decisions ??= new(StringComparer.InvariantCultureIgnoreCase))[path] = visibility;
    }
    
    public void ScopeCollection<TStorage, TElement>(
        Expression<Func<TStorage, IEnumerable<TElement>>> collection,
        Expression<Func<TElement, bool>> predicate)
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(predicate);

        ValidateCollectionSelector(collection);
        ValidateCollectionElementType<TElement>(collection);

        (_collectionScopes ??= []).Add(
            new RqlCollectionScope(collection, predicate));
    }

    private static void ValidateCollectionSelector(LambdaExpression selector)
    {
        Expression current = selector.Body;

        if (current is not MemberExpression)
        {
            throw new ArgumentException(
                "The collection selector must be a member path.",
                nameof(selector));
        }

        while (current is MemberExpression member)
        {
            if (member.Expression is null)
            {
                throw new ArgumentException(
                    "The collection selector cannot reference a static member.",
                    nameof(selector));
            }

            current = member.Expression;
        }

        if (current != selector.Parameters[0])
        {
            throw new ArgumentException(
                "The collection selector must be rooted in its source parameter.",
                nameof(selector));
        }
    }
    
    private static void ValidateCollectionElementType<TElement>(
        LambdaExpression collection)
    {
        var collectionType = collection.Body.Type;

        var elementTypes =
            CollectionQueryHelper.GetEnumerableElementTypes(collectionType);

        if (elementTypes.Length != 1 ||
            elementTypes[0] != typeof(TElement))
        {
            throw new ArgumentException(
                $"The collection selector must expose exactly one " +
                $"IEnumerable<T> element type matching '{typeof(TElement)}'. " +
                $"Selector type: '{collectionType}'.",
                nameof(collection));
        }
    }
    
    /// <summary>
    /// Validates collection scopes against the query's storage type.
    /// </summary>
    public void ValidateCollectionScopeSource(Type storageType)
    {
        ArgumentNullException.ThrowIfNull(storageType);

        foreach (var scope in CollectionScopes)
        {
            var registeredType = scope.CollectionSelector.Parameters[0].Type;

            if (registeredType != storageType)
            {
                throw new ArgumentException(
                    $"Collection scope source type '{registeredType}' " +
                    $"must match the query storage type '{storageType}'.",
                    "configure");
            }
        }
    }
}
