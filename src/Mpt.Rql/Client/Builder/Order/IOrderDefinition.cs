namespace Mpt.Rql.Client.Builder.Order;

internal interface IOrderDefinition
{
    string ToQuery(IPropertyVisitor propertyVisitor);
}