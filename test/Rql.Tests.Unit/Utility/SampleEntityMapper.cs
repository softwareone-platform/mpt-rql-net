using SoftwareOne.Rql;
using System.Linq.Expressions;

namespace Rql.Tests.Unit.Utility;

internal class SampleEntityMapper : IRqlMapper<SampleEntity, SampleEntityView>
{
    public Expression<Func<SampleEntity, SampleEntityView>> GetMapping()
        => t => new SampleEntityView
        {
            Id = t.Id,
            Desc = t.Description,
            Name = t.ProductName,
            Category = t.Category,
            Price = t.Price,
            SellPrice = t.SalePrice,
            ListDate = t.ListDate,
            Sub = new SampleEntityView
            {
                Id = t.Id,
                Desc = t.Description,
                Name = t.ProductName,
                Category = t.Category,
                Price = t.Price,
                SellPrice = t.SalePrice,
                ListDate = t.ListDate,
            }
        };
}