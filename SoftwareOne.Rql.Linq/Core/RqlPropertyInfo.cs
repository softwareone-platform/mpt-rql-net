using System.Reflection;

namespace SoftwareOne.Rql.Linq.Core
{
    internal class RqlPropertyInfo
    {
        public PropertyInfo Property { get; internal set; } = null!;
        public RqlPropertyType Type { get; internal set; }
        public MemberFlag Flags { get; internal set; }
    }
}
