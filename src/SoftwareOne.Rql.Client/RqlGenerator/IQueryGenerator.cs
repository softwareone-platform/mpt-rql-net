namespace SoftwareOne.Rql.Client.RqlGenerator;

public interface IQueryGenerator
{
    Rql Generate(Query query);
}