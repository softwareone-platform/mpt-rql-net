namespace Mpt.Rql.Client.Builder.Select;

internal interface ISelectDefinition
{
    string ToQuery(IPropertyVisitor propertyVisitor);
}
