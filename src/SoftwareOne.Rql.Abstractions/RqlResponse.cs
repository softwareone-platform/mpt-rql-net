#pragma warning disable IDE0130
using SoftwareOne.Rql.Abstractions;
using SoftwareOne.Rql.Abstractions.Result;

namespace SoftwareOne.Rql;

public class RqlGraphResponse
{
    public bool IsSuccess { get; set; }
    
    public List<Error> Errors { get; set; } = null!;
    
    public IRqlNode Graph { get; set; } = null!;
}


public class RqlResponse<TView> : RqlGraphResponse
{
    public IQueryable<TView> Query { get; set; } = null!;
}