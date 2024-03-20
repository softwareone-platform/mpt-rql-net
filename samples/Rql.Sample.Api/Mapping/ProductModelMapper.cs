using Rql.Sample.Contracts.Ef.Products;
using Rql.Sample.Domain.Ef;
using SoftwareOne.Rql;

namespace Rql.Sample.Api.Mapping
{
    internal class ProductModelMapper : IRqlMapper<ProductModel, ProductModelView>
    {
        public void MapEntity(IRqlMapperContext<ProductModel, ProductModelView> context)
        {
            context.Map(t => t.Name, t => t.Name);
        }
    }
}
