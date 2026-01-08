using System.Reflection;

namespace Mpt.Rql.Abstractions;

public interface IRqlPropertyInfo
{
    string Name { get; }
    PropertyInfo Property { get; }
    
    /// <summary>
    /// Gets the visibility and selection mode of the property in RQL queries.
    /// <para>
    /// - <see cref="RqlPropertyMode.Default"/>: Property follows standard selection rules (IsCore, select settings, depth limits).
    /// </para>
    /// <para>
    /// - <see cref="RqlPropertyMode.Ignored"/>: Property is completely excluded from all RQL operations (select, filter, order).
    /// </para>
    /// <para>
    /// - <see cref="RqlPropertyMode.Forced"/>: Property is always included, bypassing depth limits and exclusion attempts.
    /// </para>
    /// </summary>
    RqlPropertyMode Mode { get; }
    
    bool IsCore { get; }
    bool IsNullable { get; }
    RqlSelectModes? SelectModeOverride { get; }
    RqlActions Actions { get; }
    RqlOperators Operators { get; }
    RqlPropertyType Type { get; }
    Type? ElementType { get; }
}