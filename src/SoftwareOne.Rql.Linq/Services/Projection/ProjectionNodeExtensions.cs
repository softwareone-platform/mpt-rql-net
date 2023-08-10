using SoftwareOne.Rql.Abstractions;
using SoftwareOne.Rql.Abstractions.Constant;
using SoftwareOne.Rql.Abstractions.Group;
using SoftwareOne.Rql.Linq.Core;

namespace SoftwareOne.Rql.Linq.Services.Projection
{
    internal static class ProjectionNodeExtensions
    {
        public static ProjectionNode ToProjection(this RqlGroup srcNode)
        {
            var wrap = new ProjectionNode();

            // named generic groups are complex properties
            if (srcNode is RqlGenericGroup genGrp && !string.IsNullOrEmpty(genGrp.Name))
                wrap.AddChild(BuildFromRqlExpression(srcNode));
            else if (srcNode.Items != null)
                foreach (var child in srcNode.Items)
                    wrap.AddChild(BuildFromRqlExpression(child));

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
                            Mode = SignToSelectMode(sign)
                        };

                        if (grp.Items != null)
                            foreach (var child in grp.Items)
                            {
                                result.AddChild(BuildFromRqlExpression(child));
                            }
                        return result;
                    }
                case RqlConstant constant:
                    {
                        var (path, sign) = StringHelper.ExtractSign(constant.Value);
                        return new ProjectionNode
                        {
                            Value = path,
                            Mode = SignToSelectMode(sign)
                        };
                    }
                default:
                    throw new Exception("Usupported node type");
            };
        }

        private static void AddChild(this ProjectionNode parent, ProjectionNode child)
        {
            parent.Children ??= new Dictionary<string, ProjectionNode>(StringComparer.OrdinalIgnoreCase);
            var path = child.Value.ToString();

            if (parent.Children.TryGetValue(path, out var existing))
            {
                if (child.Children != null)
                    foreach (var item in child.Children)
                        existing.AddChild(item.Value);
            }
            else
            {
                parent.Children[path] = child;
            }
        }

        private static SelectMode SignToSelectMode(bool sign) => sign ? SelectMode.All : SelectMode.None;
    }
}
