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
using System.Linq.Expressions;

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
                TraverseGroup(target, group);
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
                TraverseBinary(target, binary);
                break;
            case RqlConstant constant:
                {
                    ProcessNode(target, constant);
                }
                break;
        }

        _builderContext.SetNode(target);
    }

    private void TraverseGroup(RqlNode target, RqlGroup group)
    {
        if (group is RqlGenericGroup functionGroup && TryTraverseFunctionGroup(target, functionGroup))
            return;

        var currentTarget = target;
        if (group is RqlGenericGroup genericGroup)
        {
            var updatedTarget = ProcessNode(target, genericGroup.Name);
            if (updatedTarget != null)
                currentTarget = updatedTarget;
        }

        if (group.Items == null)
            return;

        foreach (var item in group.Items)
            TraverseRqlExpression(currentTarget, item);
    }

    private void TraverseBinary(RqlNode target, RqlBinary binary)
    {
        TraverseRqlExpression(target, binary.Left);

        // The expression stage compares against a right-hand PROPERTY when an unquoted constant resolves to one
        // of a compatible type, and against the literal text otherwise. Mirror that decision so mapping projects
        // exactly the columns the comparison reads and never a column that merely shares a name with the literal.
        if (binary.Right is RqlConstant { IsQuoted: false } rightConstant && IsRightHandProperty(target, binary.Left, rightConstant.Value))
            ProcessNode(target, rightConstant);
        else if (binary.Right is RqlPointer rightPointer)
            TraverseRqlExpression(target, rightPointer);
    }

    /// <summary>
    /// Decides whether an unquoted right-hand constant is a property operand (to be included in the graph) or a
    /// literal, mirroring <c>BinaryExpressionBuilder</c>: the path must resolve to a primitive — or to a custom
    /// resolver carrier, whose leaf type is unknown — and, when both sides are plain primitives, the right type must
    /// be coercible to the left type; otherwise the expression stage falls back to the literal.
    /// </summary>
    private bool IsRightHandProperty(RqlNode parentNode, RqlExpression left, string rightName)
    {
        if (ResolvePrimitivePath(parentNode, rightName) is not { Resolved: true } right)
            return false;

        if (right.ResolverConsumed || right.Leaf is null)
            return true; // the carrier property is what the graph needs; the leaf type is the resolver's business

        if (left is not RqlConstant leftConstant)
            return true;

        if (ResolvePrimitivePath(parentNode, leftConstant.Value) is not { Resolved: true, ResolverConsumed: false, Leaf: { } leftLeaf })
            return true;

        return CanConvertChecked(right.Leaf.Property.PropertyType, leftLeaf.Property.PropertyType);
    }

    private readonly record struct PrimitivePath(bool Resolved, bool ResolverConsumed, RqlPropertyInfo? Leaf);

    /// <summary>
    /// Walks <paramref name="name"/> (sign stripped) segment by segment from the node's type without passing
    /// through a collection. Resolves when it ends in a primitive property, or when a segment has no CLR
    /// counterpart but the previous property carries an <c>IRqlCustomPropertyResolver</c>.
    /// </summary>
    private PrimitivePath? ResolvePrimitivePath(RqlNode parentNode, string name)
    {
        var (path, _) = StringHelper.ExtractSign(name);
        if (path.Length == 0 || path.Span.SequenceEqual("*".AsSpan()))
            return null;

        var currentType = parentNode.Property != null
            ? parentNode.Property.ElementType ?? parentNode.Property.Property.PropertyType
            : typeof(TView);

        RqlPropertyInfo? property = null;
        foreach (var segment in path.ToString().Split('.'))
        {
            if (property is { Type: RqlPropertyType.Collection })
                return null;

            if (!_metadataProvider.TryGetPropertyByDisplayName(currentType, segment, out var next))
                return property?.CustomResolver is null ? null : new PrimitivePath(true, true, property);

            if (next!.Mode == RqlPropertyMode.Ignored)
                return null;

            property = next;
            currentType = property.Property.PropertyType;
        }

        return property is not null && (property.TypeOverride ?? property.Type) == RqlPropertyType.Primitive
            ? new PrimitivePath(true, false, property)
            : null;
    }

    /// <summary>Same rule <c>BinaryExpressionBuilder</c> applies with <c>Expression.ConvertChecked</c>.</summary>
    private static bool CanConvertChecked(Type from, Type to)
    {
        try
        {
            Expression.ConvertChecked(Expression.Default(from), to);
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
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
