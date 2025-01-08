using SoftwareOne.Rql;

namespace Rql.Tests.Integration.Tests.Extensibility.Utility;

internal class ProductViewMapInvalid : IRqlMapper<object, object>
{
    public void MapEntity(IRqlMapperContext<object, object> context)
    {
        throw new NotImplementedException();
    }
}
