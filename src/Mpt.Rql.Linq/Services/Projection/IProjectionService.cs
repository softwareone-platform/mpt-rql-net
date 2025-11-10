namespace Mpt.Rql.Linq.Services.Projection;

internal interface IProjectionService<TView>
{
    void Process(string? projection);
}