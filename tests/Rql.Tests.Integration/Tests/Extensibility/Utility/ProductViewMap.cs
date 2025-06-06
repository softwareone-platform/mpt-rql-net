using Rql.Tests.Integration.Core;
using SoftwareOne.Rql;
using System.Linq.Expressions;

namespace Rql.Tests.Integration.Tests.Extensibility.Utility
{
    internal class ProductViewMap : IRqlMapper<Product, ProductView>
    {
        //public Expression<Func<Product, ProductView>> GetMapping()
        //    => t => new ProductView
        //    {
        //        Id = t.Id,
        //        Name = t.Name,
        //    };
        public void MapEntity(IRqlMapperContext<Product, ProductView> context)
        {
            context.MapStatic(t => t.Id, t => t.Id);
        }
    }
}
