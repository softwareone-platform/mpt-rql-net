using SoftwareOne.Rql.Abstractions.Result;
using SoftwareOne.Rql.Linq.Core.Result;
using System.ComponentModel;
using System.Globalization;

namespace SoftwareOne.Rql.Linq.Services.Filtering.Operators;
internal static class ConstantHelper
{
    public static Result<object> ChangeType(string value, Type type)
    {
        try
        {
            return TypeDescriptor.GetConverter(type).ConvertFrom(null, CultureInfo.InvariantCulture, value)!;
        }
        catch
        {
            return Error.Validation($"Cannot convert value: '{value}'.");
        }
    }
}
