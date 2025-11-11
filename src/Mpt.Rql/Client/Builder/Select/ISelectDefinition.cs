namespace Mpt.Rql.Linq.Client.Builder.Select;

internal interface ISelectDefinition
{
    string ToQuery(IPropertyVisitor propertyVisitor);
}
