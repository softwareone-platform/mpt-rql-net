using SoftwareOne.Rql.Linq.Client.Filter;
using SoftwareOne.Rql.Linq.Client.Order;
using SoftwareOne.Rql.Linq.Client.Select;

namespace SoftwareOne.Rql.Linq.Client;

internal interface IRqlRequestGenerator
{
    Rql.RqlRequest Generate(IFilterDefinitionProvider? filter, IOrderDefinitionProvider? order, ISelectDefinitionProvider? select);
}