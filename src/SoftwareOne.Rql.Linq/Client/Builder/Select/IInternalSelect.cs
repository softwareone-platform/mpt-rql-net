using SoftwareOne.Rql.Client;

namespace SoftwareOne.Rql.Linq.Client.Select;

internal interface IInternalSelect : ISelect
{
    string ToQuery(IPropertyVisitor propertyVisitor);
}