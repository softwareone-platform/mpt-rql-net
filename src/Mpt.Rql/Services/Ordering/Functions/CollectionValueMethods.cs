using System.Collections.Concurrent;
using System.Reflection;

namespace Mpt.Rql.Services.Ordering.Functions;

/// <summary>
/// Closed-generic <see cref="MethodInfo"/>s for <c>Enumerable.Where</c>, <c>Enumerable.Select</c>
/// and <c>Enumerable.FirstOrDefault</c>, used to build the <c>collection.Where(..).Select(..).FirstOrDefault()</c>
/// sort key of an ordering function.
/// </summary>
internal interface ICollectionValueMethods
{
    /// <summary><c>Enumerable.Where&lt;TElement&gt;(IEnumerable&lt;TElement&gt;, Func&lt;TElement, bool&gt;)</c></summary>
    MethodInfo Where { get; }

    /// <summary><c>Enumerable.Select&lt;TElement, TResult&gt;(IEnumerable&lt;TElement&gt;, Func&lt;TElement, TResult&gt;)</c></summary>
    MethodInfo Select { get; }

    /// <summary><c>Enumerable.FirstOrDefault&lt;TResult&gt;(IEnumerable&lt;TResult&gt;)</c></summary>
    MethodInfo FirstOrDefault { get; }
}

internal static class CollectionValueMethods
{
    private static readonly ConcurrentDictionary<(Type Element, Type Result), ICollectionValueMethods> _cache = new();

    /// <summary>Returns the (cached) method set for the given element and result types.</summary>
    public static ICollectionValueMethods For(Type elementType, Type resultType)
        => _cache.GetOrAdd((elementType, resultType), static key =>
            (ICollectionValueMethods)Activator.CreateInstance(
                typeof(CollectionValueMethods<,>).MakeGenericType(key.Element, key.Result))!);
}

/// <summary>
/// Obtains the method infos by assigning method groups to typed delegates, so overload
/// resolution happens at compile time and no name-based reflection scanning is needed.
/// Mirrors <c>CollectionFunctions&lt;T&gt;</c> and <c>OrderingFunctions&lt;TItem, TKey&gt;</c>.
/// </summary>
internal sealed class CollectionValueMethods<TElement, TResult> : ICollectionValueMethods
{
    private static readonly MethodInfo _where =
        ((Func<IEnumerable<TElement>, Func<TElement, bool>, IEnumerable<TElement>>)Enumerable.Where).Method;

    private static readonly MethodInfo _select =
        ((Func<IEnumerable<TElement>, Func<TElement, TResult>, IEnumerable<TResult>>)Enumerable.Select).Method;

    private static readonly MethodInfo _firstOrDefault =
        ((Func<IEnumerable<TResult>, TResult?>)Enumerable.FirstOrDefault).Method;

    public MethodInfo Where => _where;

    public MethodInfo Select => _select;

    public MethodInfo FirstOrDefault => _firstOrDefault;
}
