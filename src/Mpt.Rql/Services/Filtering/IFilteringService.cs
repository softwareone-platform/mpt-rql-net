namespace Mpt.Rql.Services.Filtering;

internal interface IFilteringService<TView>
{
    void Process(string? filter);
}