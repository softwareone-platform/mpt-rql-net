using ErrorOr;
using SoftwareOne.Rql.Abstractions;
using SoftwareOne.Rql.Abstractions.Exception;
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
        => BuildDefaultsForType(_context.Graph, typeof(TView), _selectSettings.Mode);

    private void BuildDefaultsForType(RqlNode target, Type type, RqlSelectModes currentMode)
    {
        if (target.Depth > 100)
        {
            _context.AddError(Error.Failure(description: "Extreme select depth detected. Most likely a circular dependency issue. Processing stopped."));
            return;
        }

        if (target.Depth > _selectSettings.MaxDepth)
            return;

        var properties = _metadataProvider.GetPropertiesByDeclaringType(type);

        foreach (var rqlProperty in properties)
        {
            if (rqlProperty.Property == null || rqlProperty.IsIgnored)
                continue;

            if (!_actionValidator.Validate(rqlProperty, RqlActions.Select))
            {
                target.ExcludeChild(rqlProperty, ExcludeReasons.Invisible);
                continue;
            }

            // if property already added as part of filter or order - skip unless parent already selected for some reason
            if (target.TryGetChild(rqlProperty.Name, out var propertyNode)
                && !propertyNode!.IncludeReason.HasFlag(IncludeReasons.Select)
                && (target!.IncludeReason & (IncludeReasons.Select | IncludeReasons.Default)) == 0)
            {
                continue;
            }

            if (ShouldOmitProperty(rqlProperty, currentMode) || rqlProperty.SelectMode == RqlSelectModes.None)
            {
                target.ExcludeChild(rqlProperty, ExcludeReasons.Default);
                continue;
            }

            var child = target.IncludeChild(rqlProperty, IncludeReasons.Default);
            BuildDefaultsForProperty(child, rqlProperty, rqlProperty.SelectMode);
        }
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

    private static bool ShouldOmitProperty(RqlPropertyInfo rqlProperty, RqlSelectModes currentMode)
    {
        if (currentMode == RqlSelectModes.None)
            return true;

        if (currentMode.HasFlag(RqlSelectModes.Core) && rqlProperty.IsCore)
            return false;

        var effectiveType = rqlProperty.TypeOverride ?? rqlProperty.Type;

        return effectiveType switch
        {
            RqlPropertyType.Root => false,
            RqlPropertyType.Primitive => !currentMode.HasFlag(RqlSelectModes.Primitive),
            RqlPropertyType.Reference => !currentMode.HasFlag(RqlSelectModes.Reference),
            RqlPropertyType.Collection => !currentMode.HasFlag(RqlSelectModes.Collection),
            _ => throw new NotImplementedException("Unknown RQL property type"),
        };
    }

    protected override RqlNode AddNodeToGraph(RqlNode parentNode, RqlPropertyInfo rqlProperty, bool sign)
    {
        if (sign)
        {
            var child = parentNode.IncludeChild(rqlProperty, IncludeReasons.Select);
            BuildDefaultsForProperty(child, child.Property, RqlSelectModes.All);
            return child;
        }

        return parentNode.ExcludeChild(rqlProperty, ExcludeReasons.Unselected);
    }

    protected override void OnValidationFailed(RqlNode node, RqlPropertyInfo property)
    {
        node.ExcludeChild(property!, ExcludeReasons.Invisible);
    }
}
