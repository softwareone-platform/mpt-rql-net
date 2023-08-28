using SoftwareOne.Rql.Linq.Client;

#pragma warning disable IDE0130
namespace SoftwareOne.Rql.Client;

public interface IQueryGenerator
{
    Rql Generate(Query query);
}