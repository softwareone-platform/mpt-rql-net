using System.Reflection;

namespace Mpt.Rql.Services.Ordering.Functions;

/// <summary>
/// Provides closed-generic <see cref="MethodInfo"/> references for
/// <c>Enumerable.Where</c>, <c>Enumerable.Select</c>, and <c>Enumerable.FirstOrDefault</c>.
/// </summary>
/// <remarks>
/// <para>
/// The methods are obtained through type-safe delegate capture rather than reflection
/// scanning (e.g. <c>typeof(Enumerable).GetMethods().Where(...)</c>). Assigning a method
/// group to a typed delegate resolves overload selection at compile time; reading
/// <c>.Method</c> from the resulting delegate yields the exact closed-generic
/// <see cref="MethodInfo"/> needed to build <c>Expression.Call</c> nodes — without any
/// fragile name-based or signature-based lookup.
/// </para>
/// <para>
/// This follows the same pattern as
/// <see cref="Mpt.Rql.Services.Filtering.Operators.Collection.Implementation.CollectionFunctions{T}"/>.
/// </para>
/// <para>
/// The concrete type is instantiated at runtime via
/// <c>Activator.CreateInstance(typeof(WhereSelectMethods&lt;,&gt;).MakeGenericType(elementType, resultType))</c>
/// once the element and result types are known from the parsed RQL arguments.
/// </para>
/// </remarks>
internal interface IWhereSelectMethods
{
    /// <summary>Returns <c>MethodInfo</c> for <c>Enumerable.Where&lt;TElement&gt;</c>.</summary>
    MethodInfo GetWhere();

    /// <summary>Returns <c>MethodInfo</c> for <c>Enumerable.Select&lt;TElement, TResult&gt;</c>.</summary>
    MethodInfo GetSelect();

    /// <summary>Returns <c>MethodInfo</c> for <c>Enumerable.FirstOrDefault&lt;TResult&gt;</c>.</summary>
    MethodInfo GetFirstOrDefault();
}

internal class WhereSelectMethods<TElement, TResult> : IWhereSelectMethods
{
    public MethodInfo GetWhere()
    {
        Func<IEnumerable<TElement>, Func<TElement, bool>, IEnumerable<TElement>> func = Enumerable.Where;
        return func.Method;
    }

    public MethodInfo GetSelect()
    {
        Func<IEnumerable<TElement>, Func<TElement, TResult>, IEnumerable<TResult>> func = Enumerable.Select;
        return func.Method;
    }

    public MethodInfo GetFirstOrDefault()
    {
        Func<IEnumerable<TResult>, TResult?> func = Enumerable.FirstOrDefault;
        return func.Method;
    }
}
