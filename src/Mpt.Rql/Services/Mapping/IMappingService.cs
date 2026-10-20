using System.Linq.Expressions;

namespace Mpt.Rql.Services.Mapping;

internal interface IMappingService<TStorage, TView>
{
    /// <summary>Builds the projection of the included graph nodes from the storage type to the view type.</summary>
    Expression<Func<TStorage, TView>> BuildSelector();

    IQueryable<TView> Apply(IQueryable<TStorage> query);
}
