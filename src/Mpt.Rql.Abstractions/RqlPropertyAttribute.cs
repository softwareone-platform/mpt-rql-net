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

}
