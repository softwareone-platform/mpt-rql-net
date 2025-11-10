using SoftwareOne.Rql;
using System.Reflection;

namespace Rql.Tests.Integration.Tests.Extensibility.Utility;

internal class CustomPropertyNameProvider : IPropertyNameProvider
{
    public string GetName(PropertyInfo property)
    {
        throw new NotImplementedException();
    }
}
