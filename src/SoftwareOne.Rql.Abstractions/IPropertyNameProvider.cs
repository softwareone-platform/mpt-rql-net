using System.Reflection;

#pragma warning disable IDE0130
namespace SoftwareOne.Rql;

public interface IPropertyNameProvider
{
    string GetName(PropertyInfo property);
}