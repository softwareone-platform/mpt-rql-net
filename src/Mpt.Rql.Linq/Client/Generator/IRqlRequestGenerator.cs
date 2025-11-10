using Mpt.Rql.Client;
using Mpt.Rql.Linq.Client.Builder.Order;
using Mpt.Rql.Linq.Client.Builder.Select;

namespace Mpt.Rql.Linq.Client.Generator;

internal interface IRqlRequestGenerator
{
    RqlRequest Generate(IOperator? filter, IOrderDefinitionProvider? order, ISelectDefinitionProvider? select);
}