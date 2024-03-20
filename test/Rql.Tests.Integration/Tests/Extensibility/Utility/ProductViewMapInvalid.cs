using SoftwareOne.Rql;

namespace Rql.Tests.Integration.Tests.Extensibility.Utility
{
    internal class ProductViewMapInvalid : IRqlMapper
    {
        public void MapEntity(IRqlMapperContext context)
        {
            throw new NotImplementedException();
        }
    }
}
