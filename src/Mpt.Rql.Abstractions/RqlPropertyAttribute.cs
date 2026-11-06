using Mpt.Rql.Abstractions;
using Mpt.Rql.Abstractions.Exception;

#pragma warning disable IDE0130
namespace Mpt.Rql;

[AttributeUsage(AttributeTargets.Property)]
public class RqlPropertyAttribute : Attribute
{
    private RqlActions? _actionFlags;
    private RqlOperators? _operatorFlags;
    private RqlSelectModes? _select;
    private RqlPropertyType? _treatAs;
    private bool? _isNullable;

    public RqlPropertyAttribute()
    {
    }

    public RqlPropertyAttribute(RqlActions actionFlags)
    {
        Actions = actionFlags;
    }

    public bool IsCore { get; set; }

    /// <summary>
    /// Gets or sets the visibility and selection mode of the property in RQL queries.
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
    /// <remarks>
    /// Default value is <see cref="RqlPropertyMode.Default"/>.
    /// </remarks>
    public RqlPropertyMode Mode { get; set; }

    [Obsolete("Use Mode instead.")]
    public bool IsIgnored
    {
        get => Mode == RqlPropertyMode.Ignored;
        set => Mode = value ? RqlPropertyMode.Ignored : RqlPropertyMode.Default;
    }

    public RqlActions Actions
    {
        get => _actionFlags ?? RqlActions.None;
        set
        {
            _actionFlags = value;
            ActionsSet = true;
        }
    }

    public RqlOperators Operators
    {
        get => _operatorFlags ?? RqlOperators.None;
        set
        {
            _operatorFlags = value;
            OperatorsSet = true;
        }
    }

    public RqlSelectModes Select
    {
        get => _select ?? RqlSelectModes.None;
        set
        {
            _select = value;
            SelectSet = true;
        }
    }

    public RqlPropertyType TreatAs
    {
        get => _treatAs ?? RqlPropertyType.Primitive;
        set
        {
            _treatAs = value;
            TreatAsSet = true;
        }
    }

    /// <summary>
    /// Specifies that property can be null. Overrides default nullability detection.
    /// </summary>
    public bool IsNullable
    {
        get => _isNullable ?? false;
        set
        {
            _isNullable = value;
            IsNullableSet = true;
        }
    }

    public bool ActionsSet { get; private set; }

    public bool OperatorsSet { get; private set; }

    public bool SelectSet { get; private set; }

    public bool TreatAsSet { get; private set; }

    public bool IsNullableSet { get; private set; }

    /// <summary>
    /// Type used to define property actions at runtime
    /// Must implement <see cref="IActionStrategy">
    /// </summary>
    /// <exception cref="RqlInvalidActionStrategyException">Thrown when provided type does not implement <see cref="IActionStrategy"</exception>
    public Type? ActionStrategy { get; set; }

    /// <summary>
    /// Type used to translate RQL sub-paths under this property — e.g. keys of a dynamic
    /// bag whose children aren't visible to CLR reflection. Must implement
    /// <see cref="IRqlCustomPropertyResolver"/> and be registered in DI.
    /// </summary>
    /// <exception cref="RqlInvalidCustomResolverException">Thrown when provided type does not implement <see cref="IRqlCustomPropertyResolver"/>.</exception>
    public Type? CustomResolver { get; set; }

    /// <summary>
    /// Names of the properties of the same type that this property reads, such as the inputs of a computed getter.
    /// Use <c>nameof</c>.
    /// Whenever this property is projected, they are projected with it,
    /// whatever their own select settings and actions say, so the getter has values to read.
    /// They are not projected on their own account,
    /// and a property projected for this reason alone carries only the <see cref="IncludeReasons.Dependency"/> include reason,
    /// so a response writer can leave it out.
    /// Filtering and ordering are not affected.
    /// On a property declared <see cref="RqlPropertyMode.Ignored"/> the names are still checked, but nothing is expanded from them.
    /// </summary>
    /// <exception cref="RqlInvalidDependencyException">
    /// Thrown when the type's metadata is first built,
    /// if a name is this property itself, is not a property of the type, names an ignored property or one that is not primitive.
    /// </exception>
    public string[]? DependsOn { get; set; }
}
