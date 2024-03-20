using ErrorOr;

namespace SoftwareOne.Rql.Linq.Services.Projection;

internal interface IProjectionService<TView>
{
    void Process(string? projection);
}