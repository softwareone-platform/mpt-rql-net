using SoftwareOne.Rql.Client.Exceptions;
using SoftwareOne.Rql.Linq.Client.Builder.Paging;

namespace SoftwareOne.Rql.Linq.Client.RqlGenerator;

public class PagingGenerator : IPagingGenerator
{
    public string Generate(Paging paging)
    {
        return paging switch
        {
            DefaultPaging => null!,
            CustomPaging p => $"limit={p.Limit}&offset={p.Offset}",
            _ => throw new InvalidDefinitionException($"Type {paging.GetType()} is not supported")
        };
    }
}