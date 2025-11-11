namespace Mpt.Rql.Linq.Core.Metadata;

internal static class TypeHelper
{
    public static bool IsUserComplexType(Type type)
    {
        return type.IsClass && !type.FullName!.StartsWith("System.");
    }
}
