using ErrorOr;
using System.ComponentModel;
using System.Globalization;

namespace SoftwareOne.Rql.Linq.Services.Filtering.Operators;
internal static class ConstantHelper
{
    public static ErrorOr<object> ChangeType(string value, Type type)
    {
        try
        {
            return TypeDescriptor.GetConverter(type).ConvertFrom(null, CultureInfo.InvariantCulture, value)!;
        }
        catch
        {
            return Error.Validation(description: $"Cannot convert value: '{value}'.");
        }
    }
}
