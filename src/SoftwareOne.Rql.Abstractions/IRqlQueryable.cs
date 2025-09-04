#pragma warning disable IDE0130
namespace SoftwareOne.Rql;

public interface IRqlQueryable
{
    RqlGraphResponse BuildGraph(RqlRequest request);
}
