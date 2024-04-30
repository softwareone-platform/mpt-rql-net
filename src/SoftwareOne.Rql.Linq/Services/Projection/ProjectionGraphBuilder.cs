using ErrorOr;
using SoftwareOne.Rql.Abstractions;
using SoftwareOne.Rql.Linq.Configuration;
using SoftwareOne.Rql.Linq.Core;
using SoftwareOne.Rql.Linq.Core.Metadata;
using SoftwareOne.Rql.Linq.Services.Context;
using SoftwareOne.Rql.Linq.Services.Graph;

namespace SoftwareOne.Rql.Linq.Services.Projection;

internal interface IProjectionGraphBuilder<TView> : IGraphBuilder<TView>
{
    void BuildDefaults();
}

internal class ProjectionGraphBuilder<TView> : GraphBuilder<TView>, IProjectionGraphBuilder<TView>
{
    private readonly IQueryContext<TView> _context;
    private readonly IMetadataProvider _metadataProvider;
    private readonly IActionValidator _actionValidator;
    private readonly IRqlSelectSettings _selectSettings;

    protected override RqlActions Action => RqlActions.Select;

    public ProjectionGraphBuilder(IQueryContext<TView> context, IMetadataProvider metadataProvider, IActionValidator actionValidator, IRqlSelectSettings selectSettings)
        : base(metadataProvider, actionValidator)
    {
        _context = context;
        _metadataProvider = metadataProvider;
        _actionValidator = actionValidator;
        _selectSettings = selectSettings;
    }

    public void BuildDefaults()
        => BuildDefaultsForType(_context.Graph, typeof(TView), _selectSettings.Explicit);

    private void BuildDefaultsForType(RqlNode target, Type type, RqlSelectModes currentMode)
    {
        if (MaxDepthExceeded(target))
            return;

        var properties = _metadataProvider.GetPropertiesByDeclaringType(type);

        foreach (var rqlProperty in properties)
        {
            // invalid and ignored properties are skipped
            if (rqlProperty.Property == null || rqlProperty.IsIgnored)
                continue;

            // properties which don't pass select validation excluded as invisible
            if (!_actionValidator.Validate(rqlProperty, RqlActions.Select))
            {
                target.ExcludeChild(rqlProperty, ExcludeReasons.Invisible);
                continue;
            }

            // if property should be omitted add exclude reason
            if (ShouldOmitProperty(rqlProperty, currentMode))
            {
                target.ExcludeChild(rqlProperty, ExcludeReasons.Default);
                continue;
            }

            // if property survives all checks it gets added as default
            var child = target.IncludeChild(rqlProperty, IncludeReasons.Default);

            // continue hierarchical select for survivor properties that was not deselected explicitly
            if (!child.ExcludeReason.HasFlag(ExcludeReasons.Unselected))
            {
                BuildDefaultsForProperty(child, rqlProperty, child.IncludeReason.HasFlag(IncludeReasons.Select) ? _selectSettings.Explicit : rqlProperty.SelectModeOverride ?? _selectSettings.Implicit);
            }
        }
    }

    private bool MaxDepthExceeded(RqlNode target)
    {
        if (target.Depth > 100)
        {
            _context.AddError(Error.Failure(description: "Extreme select depth detected. Most likely a circular dependency issue. Processing stopped."));
            return true;
        }

        if (target.Depth > _selectSettings.MaxDepth)
            return true;

        return false;
    }

    private void BuildDefaultsForProperty(RqlNode target, IRqlPropertyInfo rqlProperty, RqlSelectModes mode)
    {
        switch (rqlProperty.Type)
        {
            case RqlPropertyType.Reference:
                {
                    BuildDefaultsForType(target, rqlProperty.Property.PropertyType, mode);
                    break;
                }
            case RqlPropertyType.Collection:
                {
                    BuildDefaultsForType(target, rqlProperty.ElementType!, mode);
                    break;
                }
            default:
                break;
        }
    }

    private static bool ShouldOmitProperty(RqlPropertyInfo rqlProperty, RqlSelectModes parentMode)
    {
        if (parentMode == RqlSelectModes.None || rqlProperty.SelectModeOverride == RqlSelectModes.None)
            return true;

        if (parentMode.HasFlag(RqlSelectModes.Core) && rqlProperty.IsCore)
            return false;

        var effectiveType = rqlProperty.TypeOverride ?? rqlProperty.Type;

        return effectiveType switch
        {
            RqlPropertyType.Root => false,
            RqlPropertyType.Primitive => !parentMode.HasFlag(RqlSelectModes.Primitive),
            RqlPropertyType.Reference => !parentMode.HasFlag(RqlSelectModes.Reference),
            RqlPropertyType.Collection => !parentMode.HasFlag(RqlSelectModes.Collection),
            _ => throw new NotImplementedException("Unknown RQL property type"),
        };
    }

    protected override RqlNode AddNodeToGraph(RqlNode parentNode, RqlPropertyInfo rqlProperty, bool sign)
    {
        if (sign)
        {
            var child = parentNode.IncludeChild(rqlProperty, IncludeReasons.Select);

            // extend configured select mode with explicit config
            var selectMode = rqlProperty.SelectModeOverride.HasValue ? (rqlProperty.SelectModeOverride.Value | _selectSettings.Explicit) : _selectSettings.Explicit;
            BuildDefaultsForProperty(child, child.Property, selectMode);
            return child;
        }

        return parentNode.ExcludeChild(rqlProperty, ExcludeReasons.Unselected);
    }

    protected override void OnValidationFailed(RqlNode node, RqlPropertyInfo property)
    {
        node.ExcludeChild(property!, ExcludeReasons.Invisible);
    }
}
