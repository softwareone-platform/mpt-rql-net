using Mpt.Rql.Core;
using System.Reflection;

namespace Mpt.Rql.Core.Metadata;
internal interface IMetadataFactory
{
    RqlPropertyInfo MakeRqlPropertyInfo(string name, PropertyInfo property);
}
