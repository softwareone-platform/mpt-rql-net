using SoftwareOne.Rql.Client.Builder.Paging;

namespace SoftwareOne.Rql.Client.RqlGenerator;

public class PagingGenerator : IPagingGenerator
{
    public string Generate(Paging paging)
    {
        return paging switch
        {
            DefaultPaging => string.Empty,
            CustomPaging p => $"limit={p.Limit}&offset={p.Offset}"
        };
    }
}