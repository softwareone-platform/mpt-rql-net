﻿using SoftwareOne.Rql.Abstractions;
using SoftwareOne.Rql.Abstractions.Argument;
using SoftwareOne.Rql.Abstractions.Argument.Pointer;
using SoftwareOne.Rql.Abstractions.Binary;
using SoftwareOne.Rql.Abstractions.Collection;
using SoftwareOne.Rql.Abstractions.Group;
using SoftwareOne.Rql.Abstractions.Unary;
using SoftwareOne.Rql.Linq.Core;
using SoftwareOne.Rql.Linq.Core.Metadata;
using SoftwareOne.Rql.Linq.Services.Context;

namespace SoftwareOne.Rql.Linq.Services.Graph;

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

    private RqlNode? ProcessNode(RqlNode parentNode, RqlExpression constant, bool hierarchyOnly = false)
    {
        if (constant is not RqlConstant constExpression)
            return null;

        return ProcessNode(parentNode, constExpression.Value, hierarchyOnly);
    }

    private RqlNode? ProcessNode(RqlNode parentNode, string name, bool hierarchyOnly = false)
    {
        var currentType = parentNode.Property != null ? parentNode.Property.ElementType ?? parentNode.Property.Property.PropertyType : typeof(TView);

        var (path, sign) = StringHelper.ExtractSign(name);

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
