using Mpt.Rql.Abstractions;
using Mpt.Rql.Abstractions.Configuration;
using Mpt.Rql.Abstractions.Result;
using Mpt.Rql.Core;
using Mpt.Rql.Core.Metadata;
using Mpt.Rql.Services.Context;
using Mpt.Rql.Services.Graph;
using Mpt.Rql.Settings;

namespace Mpt.Rql.Services.Projection;

internal interface IProjectionGraphBuilder<TView> : IGraphBuilder<TView>
{
    void BuildDecisions();

    void BuildDefaults();
}

internal class ProjectionGraphBuilder<TView> : GraphBuilder<TView>, IProjectionGraphBuilder<TView>
{
    private readonly IQueryContext<TView> _context;
    private readonly IMetadataProvider _metadataProvider;
    private readonly IActionValidator _actionValidator;
    private readonly IRqlSettings _settings;
    private readonly RqlTransformOptions _options;
    private HashSet<RqlNode>? _decidedParents;
    private List<RqlNode>? _shown;

    protected override RqlActions Action => RqlActions.Select;

    public ProjectionGraphBuilder(IQueryContext<TView> context, IMetadataProvider metadataProvider, IActionValidator actionValidator, IBuilderContext builderContext, IRqlSettings settings, RqlTransformOptions options)
        : base(metadataProvider, actionValidator, builderContext)
    {
        _context = context;
        _metadataProvider = metadataProvider;
        _actionValidator = actionValidator;
        _settings = settings;
        _options = options;
    }

    public void BuildDecisions()
    {
        if (_options.Decisions == null)
            return;

        foreach (var (path, visibility) in _options.Decisions)
            BuildDecision(path, visibility);
    }

    private void BuildDecision(string path, RqlVisibility visibility)
    {
        var segments = path.Split('.');
        var properties = new RqlPropertyInfo[segments.Length];
        var type = typeof(TView);

        for (var i = 0; i < segments.Length; i++)
        {
            // a path that is not a property RQL builds is left alone
            if (!_metadataProvider.TryGetPropertyByDisplayName(type, segments[i], out var rqlProperty) || rqlProperty!.Property == null || rqlProperty.Mode == RqlPropertyMode.Ignored)
                return;

            properties[i] = rqlProperty;
            type = rqlProperty.ElementType ?? rqlProperty.Property.PropertyType;
        }

        // the properties above the one decided on are added as left out by default, which leaves them to the request
        // and the defaults
        var target = _context.Graph;
        foreach (var rqlProperty in properties[..^1])
            target = target.ExcludeChild(rqlProperty, ExcludeReasons.Default);

        // a shown property joins the selection once the request is known, see BuildShown
        if (visibility == RqlVisibility.Shown)
            (_shown ??= []).Add(target.IncludeChild(properties[^1], IncludeReasons.Override));
        else
            target.ExcludeChild(properties[^1], ExcludeReasons.Override);

        (_decidedParents ??= []).Add(target);
    }

    public void BuildDefaults()
    {
        BuildDefaultsForType(_context.Graph, typeof(TView), _settings.Select.Explicit);
        BuildShown();
    }

    /// <summary>
    /// Brings the shown properties into the selection once the request is known, top down, wherever RQL built the
    /// property above them and the request deselects neither them nor it.
    /// </summary>
    private void BuildShown()
    {
        if (_shown == null)
            return;

        foreach (var node in _shown.OrderBy(t => t.Depth))
        {
            var parent = (RqlNode)node.Parent!;
            if (parent.AppliedMode == null || IsDeselected(parent) || IsDeselected(node))
                continue;

            node.AddIncludeReason(IncludeReasons.Default);
            BuildSelectedDefaults(node);
        }
    }

    private void BuildDefaultsForType(RqlNode target, Type type, RqlSelectModes currentMode)
    {
        if (MaxDepthExceeded(target))
            return;

        if (target.AppliedMode?.HasFlag(currentMode) == true)
            return;

        var properties = _metadataProvider.GetPropertiesByDeclaringType(type);

        foreach (var rqlProperty in properties)
        {
            // invalid and ignored properties are skipped
            if (rqlProperty.Property == null)
                continue;

            bool shouldContinueToNextProperty = false;
            switch (rqlProperty.Mode)
            {
                case RqlPropertyMode.Default:
                    break;
                case RqlPropertyMode.Ignored:
                    shouldContinueToNextProperty = true;
                    break;
                case RqlPropertyMode.Forced:
                    target.IncludeChild(rqlProperty, IncludeReasons.Forced);
                    break;
            }

            if (shouldContinueToNextProperty)
            {
                continue;
            }

            // properties with a visibility set for the call skip the checks below: hidden ones stay out unless forced
            // above, and shown ones join the selection once the request is known
            if (FindVisibility(target, rqlProperty) != null)
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
                BuildDefaultsForProperty(child, rqlProperty, child.IncludeReason.HasFlag(IncludeReasons.Select) ? _settings.Select.Explicit : rqlProperty.SelectModeOverride ?? _settings.Select.Implicit);
            }
        }
        target.AppliedMode = (target.AppliedMode ?? RqlSelectModes.None) | currentMode;
    }

    private bool MaxDepthExceeded(RqlNode target)
    {
        if (target.Depth > 100)
        {
            _context.AddError(Error.General("Extreme select depth detected. Most likely a circular dependency issue. Processing stopped."));
            return true;
        }

        if (target.Depth > _settings.Select.MaxDepth)
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
            BuildSelectedDefaults(child);
            return child;
        }
        else
        {
            var child = parentNode.ExcludeChild(rqlProperty, ExcludeReasons.Unselected);
            BuildDefaultsForProperty(child, child.Property, RqlSelectModes.None);
            return child;
        }
    }

    private void BuildSelectedDefaults(RqlNode node)
    {
        // extend configured select mode with explicit config
        var selectMode = node.Property.SelectModeOverride.HasValue ? node.Property.SelectModeOverride.Value | _settings.Select.Explicit : _settings.Select.Explicit;
        BuildDefaultsForProperty(node, node.Property, selectMode);
    }

    // deselected by the request, which also selecting the property overrules
    private static bool IsDeselected(RqlNode node)
        => node.ExcludeReason.HasFlag(ExcludeReasons.Unselected) && !node.IncludeReason.HasFlag(IncludeReasons.Select);

    // the visibility set on the property beneath the node, which BuildDecisions put in the graph before anything else
    private RqlVisibility? FindVisibility(RqlNode parentNode, RqlPropertyInfo rqlProperty)
    {
        if (_decidedParents?.Contains(parentNode) != true || !parentNode.TryGetChild(rqlProperty.Name, out var child))
            return null;

        if (child!.IncludeReason.HasFlag(IncludeReasons.Override))
            return RqlVisibility.Shown;

        return child.ExcludeReason.HasFlag(ExcludeReasons.Override) ? RqlVisibility.Hidden : null;
    }

    protected override void OnNodeAddedDueToHierarchy(RqlNode node, RqlPropertyInfo property)
    {
        BuildDefaultsForProperty(node, property, RqlSelectModes.None);
    }

    protected override bool IsAllowed(RqlNode parentNode, RqlPropertyInfo rqlProperty)
        => FindVisibility(parentNode, rqlProperty) is { } visibility ? visibility == RqlVisibility.Shown : base.IsAllowed(parentNode, rqlProperty);

    protected override void OnValidationFailed(RqlNode node, RqlPropertyInfo property)
    {
        // a property hidden for the call already carries its reason
        if (FindVisibility(node, property) != RqlVisibility.Hidden)
            node.ExcludeChild(property!, ExcludeReasons.Invisible);
    }
}
