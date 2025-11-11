namespace Mpt.Rql.Services.Projection;

internal interface IProjectionService<TView>
{
    void Process(string? projection);
}