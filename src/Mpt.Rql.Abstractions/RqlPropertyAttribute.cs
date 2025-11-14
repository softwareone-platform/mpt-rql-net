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

    public bool IsIgnored { get; set; }

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
