using SoftwareOne.Rql.Linq.Client.Core;

namespace SoftwareOne.Rql.Linq.Client.Builder.Order;

internal interface IOrderDefinition
{
    string ToQuery(IPropertyVisitor propertyVisitor);
}