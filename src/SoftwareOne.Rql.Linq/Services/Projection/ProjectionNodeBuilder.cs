using SoftwareOne.Rql.Abstractions;
using SoftwareOne.Rql.Abstractions.Constant;
using SoftwareOne.Rql.Abstractions.Group;
using SoftwareOne.Rql.Linq.Core;

namespace SoftwareOne.Rql.Linq.Services.Projection
{
    internal static class ProjectionNodeBuilder
    {
        public static ProjectionNode Build(RqlGroup srcNode)
        {
            var wrap = new ProjectionNode();

            // named generic groups are complex properties
            if (srcNode is RqlGenericGroup genGrp && !string.IsNullOrEmpty(genGrp.Name))
                wrap.MergeChild(BuildFromRqlExpression(srcNode));
            else if (srcNode.Items != null)
                foreach (var child in srcNode.Items)
                    wrap.MergeChild(BuildFromRqlExpression(child));

            return wrap;
        }

        private static ProjectionNode BuildFromRqlExpression(RqlExpression srcNode, bool? transientSign = null)
        {
            switch (srcNode)
            {
                case RqlGenericGroup grp:
                    {
                        var (path, sign) = StringHelper.ExtractSign(grp.Name);

                        var result = new ProjectionNode
                        {
                            Value = path,
                            Type = ProjectionNodeType.None,
                            Sign = transientSign ?? sign
                        };

                        if (grp.Items != null)
                            foreach (var child in grp.Items)
                            {
                                result.MergeChild(BuildFromRqlExpression(child));
                            }
                        return result;
                    }
                case RqlConstant constant:
                    {
                        var (path, sign) = StringHelper.ExtractSign(constant.Value);
                        return BuildFromPath(path, transientSign ?? sign);
                    }
                default:
                    throw new Exception("Usupported node type");
            };
        }

        private static ProjectionNode BuildFromPath(ReadOnlyMemory<char> path, bool sign)
        {
            var dotIndex = path.Span.IndexOf(".");

            var node = new ProjectionNode { Type = ProjectionNodeType.Value, Sign = sign };
            if (dotIndex >= 0)
            {
                node.Value = path[..dotIndex];
                node.Type = ProjectionNodeType.None;
                node.MergeChild(BuildFromPath(path[(dotIndex + 1)..], sign));
            }
            else
            {
                node.Type = ProjectionNodeType.Value;
                node.Value = path;
            }
            return node;
        }

        private static void MergeChild(this ProjectionNode parent, ProjectionNode child)
        {
            if (child.Value.Span.Equals("*", StringComparison.InvariantCultureIgnoreCase))
            {
                parent.Type = ProjectionNodeType.Defaults;
                return;
            }

            parent.Children ??= new Dictionary<string, ProjectionNode>(StringComparer.OrdinalIgnoreCase);
            var path = child.Value.ToString();

            if (parent.Children.TryGetValue(path, out var existing))
            {
                if (child.Children != null)
                    foreach (var item in child.Children)
                        existing.MergeChild(item.Value);

                if (existing.Type != ProjectionNodeType.Defaults)
                    existing.Type = child.Type;

                if (child.Sign)
                    existing.Sign = child.Sign;
            }
            else
            {
                parent.Children[path] = child;
                if (child.Sign)
                    parent.Sign = child.Sign;
            }
        }
    }
}
