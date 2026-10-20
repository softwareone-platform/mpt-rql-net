namespace Mpt.Rql.Abstractions.Configuration.Filter;

/// <summary>
/// Defines where the filter predicate is applied when the source is mapped to a view type.
/// </summary>
public enum FilterBinding
{
    /// <summary>
    /// The predicate is applied to the projected view, after the mapping <c>Select</c>. A filter through a
    /// nullable nested reference then reaches a relational provider as a conditional (<c>CASE WHEN</c>)
    /// around the compared column, which prevents the use of an index on that column.
    /// </summary>
    View,

    /// <summary>
    /// The predicate is rewritten onto the storage type and applied before the mapping <c>Select</c>. The null
    /// guards of the projection become boolean terms (<c>x != null &amp;&amp; x.Member == value</c>) around a plain
    /// member access, so a relational provider translates the comparison to a plain column predicate and
    /// can use an index. The rewrite preserves the C# meaning of every operator, so results are the same
    /// as with <see cref="View"/>; the setting has no effect on a transparent mapping.
    /// </summary>
    Storage
}
