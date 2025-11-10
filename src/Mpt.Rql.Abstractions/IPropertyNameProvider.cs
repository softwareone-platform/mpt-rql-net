using System.Reflection;

#pragma warning disable IDE0130
namespace Mpt.Rql;

public interface IPropertyNameProvider
{
    string GetName(PropertyInfo property);
}