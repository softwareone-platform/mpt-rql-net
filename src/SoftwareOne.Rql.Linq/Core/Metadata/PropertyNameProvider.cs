using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SoftwareOne.Rql.Linq.Core.Metadata;

internal class PropertyNameProvider : IPropertyNameProvider
{
    public string GetName(PropertyInfo property)
    {
        var attribute = property.GetCustomAttribute<JsonPropertyNameAttribute>();
        return attribute != null ? attribute.Name : JsonNamingPolicy.CamelCase.ConvertName(property.Name);
    }
}