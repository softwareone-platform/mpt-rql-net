using SoftwareOne.Rql.Linq.Client.Builder.Order;

namespace SoftwareOne.Rql.Linq.Client.Generator;

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

        return string.Join(",", definition.Select(e => e.ToQuery(_propertyVisitor)));
    }
}