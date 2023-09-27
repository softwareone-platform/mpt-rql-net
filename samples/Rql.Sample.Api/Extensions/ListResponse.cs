#pragma warning disable IDE0130
namespace SoftwareOne.Rql;

public class ListResponse<T>
{
    public PaginationMetadata Pagination { get; set; } = null!;

    public List<T> Data { get; set; } = null!;
}
