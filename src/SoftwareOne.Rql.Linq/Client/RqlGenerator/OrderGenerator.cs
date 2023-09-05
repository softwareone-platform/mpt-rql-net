using SoftwareOne.Rql.Client;
using SoftwareOne.Rql.Linq.Client.Order;

namespace SoftwareOne.Rql.Linq.Client;

internal class OrderGenerator : IOrderGenerator
{
    private readonly IPropertyVisitor _propertyVisitor;

    public OrderGenerator(IPropertyVisitor propertyVisitor)
    {
        _propertyVisitor = propertyVisitor;
    }

    public string? Generate(IOrderDefinitionProvider? order)
    {
        if (order == null)
            return default;

        var definition = order.GetDefinition();

        if (definition == null)
            return default;

        if (definition.Any(e => e is not IInternalOrder))
        {
            throw new InvalidDefinitionException("Only for internal usage available");
        }

        return string.Join(",", definition.Cast<IInternalOrder>().Select(e => e.ToQuery(_propertyVisitor)));
    }
}