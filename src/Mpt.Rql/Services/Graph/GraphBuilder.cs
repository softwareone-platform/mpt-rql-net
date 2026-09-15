using Mpt.Rql.Abstractions;
using Mpt.Rql.Abstractions.Argument;
using Mpt.Rql.Abstractions.Argument.Pointer;
using Mpt.Rql.Abstractions.Binary;
using Mpt.Rql.Abstractions.Collection;
using Mpt.Rql.Abstractions.Group;
using Mpt.Rql.Abstractions.Unary;
using Mpt.Rql.Core;
using Mpt.Rql.Core.Metadata;
using Mpt.Rql.Services.Context;

namespace Mpt.Rql.Services.Graph;

internal abstract class GraphBuilder<TView> : IGraphBuilder<TView>
{
    private readonly IMetadataProvider _metadataProvider;
    private readonly IActionValidator _actionValidator;
    private readonly IBuilderContext _builderContext;

    protected GraphBuilder(IMetadataProvider metadataProvider, IActionValidator actionValidator, IBuilderContext builderContext)
    {
        _metadataProvider = metadataProvider;
        _actionValidator = actionValidator;
        _builderContext = builderContext;
    }

    public void TraverseRqlExpression(RqlNode? target, RqlExpression? expression)
    {
        if (target == null || expression == null) return;

        switch (expression)
        {
            case RqlGroup group:
                {
                    if (group is RqlGenericGroup functionGroup && TryTraverseFunctionGroup(target, functionGroup))
                        break;

                    var currentTarget = target;
                    if (group is RqlGenericGroup genericGroup)
                    {
                        var updatedTarget = ProcessNode(target, genericGroup.Name);
                        if (updatedTarget != null)
                            currentTarget = updatedTarget;
                    }

                    if (group.Items != null)
                        foreach (var item in group.Items)
                        {
                            TraverseRqlExpression(currentTarget, item);
                        }
                }
                break;
            case RqlUnary unary:
                TraverseRqlExpression(target, unary.Nested);
                break;
            case RqlPointer pointer:
                TraverseRqlExpression(target, pointer.Inner);
                break;
            case RqlCollection collection:
                {
                    var child = ProcessNode(target, collection.Left, true);
                    TraverseRqlExpression(child, collection.Right);
                }
                break;
            case RqlBinary binary:
                {
                    TraverseRqlExpression(target, binary.Left);

                    // The expression stage resolves an unquoted right-hand constant as a property path when one
                    // matches (property-to-property comparison); include that column too so mapping projects it.
                    // Only a fully resolvable path ending in a primitive qualifies — a literal that merely collides
                    // with a navigation name must not drag that subtree into the projection.
                    if (binary.Right is RqlConstant { IsQuoted: false } rightConstant && ResolvesToPrimitive(target, rightConstant.Value))
                        ProcessNode(target, rightConstant);
                    else if (binary.Right is RqlPointer rightPointer)
                        TraverseRqlExpression(target, rightPointer);
                }
                break;
            case RqlConstant constant:
                {
                    ProcessNode(target, constant);
                }
                break;
        }

        _builderContext.SetNode(target);
    }

    /// <summary>
    /// True when <paramref name="name"/> (sign stripped) resolves segment by segment from the node's type,
    /// without passing through a collection, to a primitive property — i.e. it can be a comparison operand.
    /// </summary>
    private bool ResolvesToPrimitive(RqlNode parentNode, string name)
    {
        var (path, _) = StringHelper.ExtractSign(name);
        if (path.Length == 0 || path.Span.SequenceEqual("*".AsSpan()))
            return false;

        var currentType = parentNode.Property != null
            ? parentNode.Property.ElementType ?? parentNode.Property.Property.PropertyType
            : typeof(TView);

        RqlPropertyInfo? property = null;
        foreach (var segment in path.ToString().Split('.'))
        {
            if (property is { Type: RqlPropertyType.Collection })
                return false;

            if (!_metadataProvider.TryGetPropertyByDisplayName(currentType, segment, out property) || property!.Mode == RqlPropertyMode.Ignored)
                return false;

            currentType = property.Property.PropertyType;
        }

        return property is not null && (property.TypeOverride ?? property.Type) == RqlPropertyType.Primitive;
    }

    protected RqlNode? ProcessNode(RqlNode parentNode, RqlExpression constant, bool hierarchyOnly = false)
    {
        if (constant is not RqlConstant constExpression)
            return null;

        return ProcessNode(parentNode, constExpression.Value, hierarchyOnly);
    }

    private RqlNode? ProcessNode(RqlNode parentNode, string name, bool hierarchyOnly = false)
    {
        var (path, sign) = StringHelper.ExtractSign(name);
        return ProcessNode(parentNode, path, sign, hierarchyOnly);
    }

    private RqlNode? ProcessNode(RqlNode parentNode, ReadOnlyMemory<char> path, bool sign, bool hierarchyOnly = false)
    {
        var currentType = parentNode.Property != null
            ? parentNode.Property.ElementType ?? parentNode.Property.Property.PropertyType
            : typeof(TView);

        if (!path.Span.SequenceEqual("*".AsSpan()))
            return ProcessNodeInternal(parentNode, path, sign, hierarchyOnly);

        var properties = _metadataProvider.GetPropertiesByDeclaringType(currentType);
        foreach (var property in properties)
        {
            ProcessNodeInternal(parentNode, property.Name.AsMemory(), sign, hierarchyOnly);
        }

        return null;
    }
    private RqlNode? ProcessNodeInternal(RqlNode parentNode, ReadOnlyMemory<char> path, bool sign, bool hierarchyOnly = false)
    {
        var currentType = parentNode.Property != null
            ? parentNode.Property.ElementType ?? parentNode.Property.Property.PropertyType
            : typeof(TView);

        var currentNode = parentNode;
        var segments = GetProperties(currentType, path).ToList();

        for (int i = 0; i < segments.Count; i++)
        {
            var rqlProperty = segments[i];

            if (!_actionValidator.Validate(rqlProperty, Action))
            {
                OnValidationFailed(currentNode, rqlProperty);
                return null;
            }

            if (i < segments.Count - 1 || hierarchyOnly) // part of the path 
            {
                currentNode = currentNode.IncludeChild(rqlProperty, IncludeReasons.Hierarchy);
                OnNodeAddedDueToHierarchy(currentNode, rqlProperty);
            }
            else // leaf
            {
                currentNode = AddNodeToGraph(currentNode, rqlProperty!, sign);

                // root of the node to be added as hierarchy
                if (i == 0)
                    parentNode.AddIncludeReason(IncludeReasons.Hierarchy);
            }
        }

        return currentNode != parentNode ? currentNode : null;
    }

    protected abstract RqlActions Action { get; }

    protected abstract RqlNode AddNodeToGraph(RqlNode parentNode, RqlPropertyInfo rqlProperty, bool sign);

    protected virtual void OnValidationFailed(RqlNode node, RqlPropertyInfo property) { }

    protected virtual void OnNodeAddedDueToHierarchy(RqlNode node, RqlPropertyInfo property) { }

    /// <summary>
    /// Gives derived builders a chance to interpret a named generic group as a function call
    /// (e.g. ordering's <c>first(...)</c>). Return <c>true</c> when the group has been handled; the
    /// base traversal — which treats the group's items as property paths — is then skipped.
    /// </summary>
    protected virtual bool TryTraverseFunctionGroup(RqlNode target, RqlGenericGroup group) => false;

    private IEnumerable<RqlPropertyInfo> GetProperties(Type type, ReadOnlyMemory<char> path)
    {
        var segments = path.ToString().Split('.');

        Type currentType = type;
        foreach (var segment in segments)
        {
            if (_metadataProvider.TryGetPropertyByDisplayName(currentType, segment, out var rqlProperty))
            {
                currentType = rqlProperty!.ElementType ?? rqlProperty.Property.PropertyType;
                yield return rqlProperty;
            }
        }
    }

}
