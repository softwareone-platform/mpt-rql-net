using Mpt.Rql.Client;
using Mpt.Rql.Client.Builder.Order;
using Mpt.Rql.Client.Builder.Select;

namespace Mpt.Rql.Client.Generator;

internal interface IRqlRequestGenerator
{
    RqlRequest Generate(IOperator? filter, IOrderDefinitionProvider? order, ISelectDefinitionProvider? select);
}