using System.Reflection;

namespace SoftwareOne.Rql.Linq.Core.Metadata
{
    internal interface IPropertyMetadataProvider
    {
        RqlPropertyInfo MakeRqlPropertyInfo(string name, PropertyInfo property, RqlMemberAttribute? typeAttribute);
    }
}
