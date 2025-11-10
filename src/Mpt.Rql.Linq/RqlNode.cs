#pragma warning disable IDE0130
using Mpt.Rql.Abstractions;
using System.Text;

namespace Mpt.Rql;

internal class RqlNode : IRqlNode
{
    private string? _fullpath = null;
    private Dictionary<string, RqlNode>? _children;

    internal RqlNode(IRqlPropertyInfo? rqlProperty, int depth)
    {
        if (rqlProperty == null) // root
        {
            Name = string.Empty;
            IncludeReason = IncludeReasons.Select;
        }
        else
        {
            Name = rqlProperty.Name;
        }

        Property = rqlProperty!;
        Depth = depth;
    }

    public string Name { get; init; }

    public IRqlPropertyInfo Property { get; private set; }

    public IncludeReasons IncludeReason { get; private set; }

    public ExcludeReasons ExcludeReason { get; private set; }

    public bool IsIncluded
    {
        get
        {
            // actively selected or participating in filter/order properties are selected
            if ((IncludeReason & (IncludeReasons.Select | IncludeReasons.Hierarchy | IncludeReasons.Filter | IncludeReasons.Order)) != 0)
                return true;

            // default properties are added unless there is good (invisible or unselected) reason to exclude them
            return IncludeReason.HasFlag(IncludeReasons.Default) && (ExcludeReason == ExcludeReasons.None || ExcludeReason == ExcludeReasons.Default);
        }
    }

    public IRqlNode? Parent { get; private set; }

    public int Depth { get; init; }

    internal RqlSelectModes? AppliedMode { get; set; }

    public bool TryGetChild(string name, out IRqlNode? child)
    {
        child = null;

        if (_children != null && _children.TryGetValue(name, out var actualChild))
        {
            child = (IRqlNode?)actualChild;
            return true;
        }

        return false;
    }

    public IEnumerable<IRqlNode> Children => _children != null ? _children.Values : Enumerable.Empty<RqlNode>();

    public int Count => _children != null ? _children.Count : 0;

    internal string DebugView => Print();

    internal static RqlNode MakeRoot() => new(null, 0);

    internal bool HasChild(IRqlPropertyInfo rqlProperty) => _children != null && _children.ContainsKey(rqlProperty.Name);

    internal RqlNode IncludeChild(IRqlPropertyInfo rqlProperty, IncludeReasons reason)
    {
        if (reason == IncludeReasons.None)
            throw new ArgumentException("Include reason must be provided", nameof(reason));

        return AddChild(rqlProperty, reason, ExcludeReasons.None);
    }

    internal RqlNode ExcludeChild(IRqlPropertyInfo rqlProperty, ExcludeReasons reason)
    {
        if (reason == ExcludeReasons.None)
            throw new ArgumentException("Exclude reason must be provided", nameof(reason));

        return AddChild(rqlProperty, IncludeReasons.None, reason);
    }

    private RqlNode AddChild(IRqlPropertyInfo rqlProperty, IncludeReasons includeReason, ExcludeReasons excludeReason)
    {
        _children ??= [];

        if (!_children.TryGetValue(rqlProperty.Name, out var child))
        {
            child = new RqlNode(rqlProperty, Depth + 1)
            {
                Parent = this,
            };
            _children.Add(rqlProperty.Name, child);
        }

        child.AddIncludeReason(includeReason);
        child.AddExcludeReason(excludeReason);
        return child;
    }

    internal void AddIncludeReason(IncludeReasons includeReason) => IncludeReason |= includeReason;

    internal void AddExcludeReason(ExcludeReasons excludeReason) => ExcludeReason |= excludeReason;

    public string GetFullPath()
    {
        if (_fullpath != null)
            return _fullpath;

        if (Parent == null || Parent.Name.Length == 0)
        {
            _fullpath = Name;
            return _fullpath;
        }

        var sb = new List<string>(4);
        var current = this;
        while (current != null)
        {
            if (current.Name.Length > 0)
            {
                sb.Add(current.Name);
            }
            current = current.Parent as RqlNode;
        }

        sb.Reverse();
        _fullpath = string.Join(".", sb);
        return _fullpath;
    }

    public string Print()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{"Node",-50}{"Incl.",-8}{"Include reason",-40}{"Exclude reason",-40}");
        sb.AppendLine(new string('-', 140));
        PrintLines(this, sb);
        return sb.ToString();

        static void PrintLines(IRqlNode node, StringBuilder sb, string indent = "", bool last = true)
        {
            var left = indent;
            if (last)
            {
                left += "└─";
                indent += "  ";
            }
            else
            {
                left += "├─";
                indent += "| ";
            }

            left += node.Parent != null ? node.Name : "root";
            var included = node.IsIncluded ? "yes" : "no";
            sb.AppendLine($"{left,-50}{included,-8}{node.IncludeReason,-40}{node.ExcludeReason,-40}");

            int i = 0;
            foreach (var child in node.Children.OrderBy(t => t.Name))
            {
                PrintLines(child, sb, indent, i == node.Count - 1);
                i++;
            }
        }
    }
}
