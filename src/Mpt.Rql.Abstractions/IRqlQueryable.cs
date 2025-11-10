using Mpt.Rql.Abstractions.Configuration;

#pragma warning disable IDE0130
namespace Mpt.Rql;

public interface IRqlQueryable
{
    RqlGraphResponse BuildGraph(RqlRequest request);

    RqlGraphResponse BuildGraph(RqlRequest request, Action<IRqlSettings> configure);
}
