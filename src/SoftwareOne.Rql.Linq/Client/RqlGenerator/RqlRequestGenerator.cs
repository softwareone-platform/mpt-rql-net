using SoftwareOne.Rql.Linq.Client.Filter;
using SoftwareOne.Rql.Linq.Client.Order;
using SoftwareOne.Rql.Linq.Client.Select;

namespace SoftwareOne.Rql.Linq.Client;

internal class RqlRequestGenerator : IRqlRequestGenerator
{
    private readonly IFilterGenerator _filterGenerator;
    private readonly ISelectGenerator _selectGenerator;
    private readonly IOrderGenerator _orderGenerator;

    public RqlRequestGenerator(IFilterGenerator filterGenerator, ISelectGenerator selectGenerator, IOrderGenerator orderGenerator)
    {
        _filterGenerator = filterGenerator;
        _selectGenerator = selectGenerator;
        _orderGenerator = orderGenerator;
    }

    public Rql.RqlRequest Generate(IFilterDefinitionProvider? filter, IOrderDefinitionProvider? order, ISelectDefinitionProvider? select)
    {
        var queryParams = _filterGenerator.Generate(filter);
        var selectParams = _selectGenerator.Generate(select);
        var orderParams = _orderGenerator.Generate(order);
        
        return new Rql.RqlRequest
        {
            Select = selectParams,
            Order = orderParams,
            Filter = queryParams
        };
    }
}