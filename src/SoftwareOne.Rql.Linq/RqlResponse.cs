#pragma warning disable IDE0130
using SoftwareOne.Rql.Linq.Core.Result;

namespace SoftwareOne.Rql;

public class RqlGraphResponse
{
    public bool IsSuccess { get; set; }
    
    public List<Error> Errors { get; set; } = null!;
    
    public RqlNode Graph { get; set; } = null!;
}


public class RqlResponse<TView> : RqlGraphResponse
{
    public IQueryable<TView> Query { get; set; } = null!;
}