using SoftwareOne.Rql.Client;
using SoftwareOne.Rql.Linq.Client.Builder.Order;
using SoftwareOne.Rql.Linq.Client.Builder.Select;

namespace SoftwareOne.Rql.Linq.Client.Generator;

internal interface IRqlRequestGenerator
{
    RqlRequest Generate(IOperator? filter, IOrderDefinitionProvider? order, ISelectDefinitionProvider? select);
}