using Mpt.Rql.Client;
using Mpt.Rql.Linq.Client.Builder.Order;
using Mpt.Rql.Linq.Client.Builder.Select;

namespace Mpt.Rql.Linq.Client.Generator;

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

    public RqlRequest Generate(IOperator? filter, IOrderDefinitionProvider? order, ISelectDefinitionProvider? select)
    {
        var queryParams = _filterGenerator.Generate(filter);
        var selectParams = _selectGenerator.Generate(select);
        var orderParams = _orderGenerator.Generate(order);

        return new RqlRequest
        {
            Select = selectParams,
            Order = orderParams,
            Filter = queryParams
        };
    }
}