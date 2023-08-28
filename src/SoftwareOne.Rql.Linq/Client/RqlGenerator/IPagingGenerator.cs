using SoftwareOne.Rql.Linq.Client.Builder.Paging;

namespace SoftwareOne.Rql.Linq.Client.RqlGenerator;

public interface IPagingGenerator
{
    string Generate(Paging paging);
}