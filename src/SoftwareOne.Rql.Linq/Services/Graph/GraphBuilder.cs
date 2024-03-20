using SoftwareOne.Rql.Abstractions;
using SoftwareOne.Rql.Abstractions.Argument;
using SoftwareOne.Rql.Abstractions.Argument.Pointer;
using SoftwareOne.Rql.Abstractions.Binary;
using SoftwareOne.Rql.Abstractions.Collection;
using SoftwareOne.Rql.Abstractions.Group;
using SoftwareOne.Rql.Abstractions.Unary;
using SoftwareOne.Rql.Linq.Core;
using SoftwareOne.Rql.Linq.Core.Metadata;

namespace SoftwareOne.Rql.Linq.Services.Graph;

internal abstract class GraphBuilder<TView> : IGraphBuilder<TView>
{
    private readonly IMetadataProvider _metadataProvider;
    private readonly IActionValidator _actionValidator;

    protected GraphBuilder(IMetadataProvider metadataProvider, IActionValidator actionValidator)
    {
        _metadataProvider = metadataProvider;
        _actionValidator = actionValidator;
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
                    var child = ProcessNode(target, collection.Left);
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
    }

    private RqlNode? ProcessNode(RqlNode parentNode, RqlExpression constant)
    {
        if (constant is not RqlConstant constExpression)
            return null;

        return ProcessNode(parentNode, constExpression.Value);
    }

    private RqlNode? ProcessNode(RqlNode parentNode, string name)
    {
        var currentType = parentNode.Property != null ? parentNode.Property.ElementType ?? parentNode.Property.Property.PropertyType : typeof(TView);

        var (path, sign) = StringHelper.ExtractSign(name);

        var currentNode = parentNode;
        foreach (var rqlProperty in GetProperties(currentType, path))
        {
            if (!_actionValidator.Validate(rqlProperty, Action))
            {
                OnValidationFailed(currentNode, rqlProperty);
            }

            currentNode = AddNodeToGraph(currentNode, rqlProperty!, sign);
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
