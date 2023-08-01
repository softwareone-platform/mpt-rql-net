using System.Reflection;

namespace SoftwareOne.Rql.Linq.Core.Metadata
{
    internal interface IPropertyMetadataProvider
    {
        RqlPropertyInfo MakeRqlPropertyInfo(PropertyInfo property, RqlMemberAttribute? typeAttribute);
    }
}
