#pragma warning disable IDE0130
using ErrorOr;

namespace SoftwareOne.Rql;

public class RqlResponse<TView>
{
    public IQueryable<TView> Query { get; set; } = null!;
    public ErrorOr<Success> Status { get; set; }
    public RqlNode Graph { get; set; } = null!;
}
