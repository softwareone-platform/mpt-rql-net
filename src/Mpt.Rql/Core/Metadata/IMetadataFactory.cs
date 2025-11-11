using System.Reflection;

namespace Mpt.Rql.Linq.Core.Metadata;
internal interface IMetadataFactory
{
    RqlPropertyInfo MakeRqlPropertyInfo(string name, PropertyInfo property);
}
