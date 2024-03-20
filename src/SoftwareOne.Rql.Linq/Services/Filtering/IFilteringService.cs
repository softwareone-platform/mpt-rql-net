using ErrorOr;

namespace SoftwareOne.Rql.Linq.Services.Filtering;

internal interface IFilteringService<TView>
{
    void Process(string? filter);
}