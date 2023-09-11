using SoftwareOne.Rql.Linq.Client.Core;

namespace SoftwareOne.Rql.Linq.Client.Builder.Select;

internal interface ISelectDefinition
{
    string ToQuery(IPropertyVisitor propertyVisitor);
}
