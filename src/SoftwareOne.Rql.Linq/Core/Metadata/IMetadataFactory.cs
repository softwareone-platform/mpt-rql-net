using System.Reflection;

namespace SoftwareOne.Rql.Linq.Core.Metadata;
internal interface IMetadataFactory
{
    RqlPropertyInfo MakeRqlPropertyInfo(string name, PropertyInfo property);
}
