using SoftwareOne.Rql;
using System.Linq.Expressions;

namespace Rql.Tests.Unit.Utility;

internal class SampleEntityMapper<TView> : IRqlMapper<SampleEntity, TView> where TView : SampleEntityView, new()
{
    //public Expression<Func<SampleEntity, TView>> GetMapping()
    //    => t => new TView
    //    {
    //        Id = t.Id,
    //        Desc = t.Description,
    //        Name = t.ProductName,
    //        Category = t.Category,
    //        Price = t.Price,
    //        SellPrice = t.SalePrice,
    //        ListDate = t.ListDate,
    //        Sub = new TView
    //        {
    //            Id = t.Id,
    //            Desc = t.Description,
    //            Name = t.ProductName,
    //            Category = t.Category,
    //            Price = t.Price,
    //            SellPrice = t.SalePrice,
    //            ListDate = t.ListDate,
    //        }
    //    };
    public void MapEntity(IRqlMapperContext<SampleEntity, TView> context)
    {
        context.Map(t => t.Id, t => t.Id);
    }
}