using Mpt.Rql.Abstractions.Result;
using Mpt.Rql.Core;
using System.ComponentModel;
using System.Globalization;

namespace Mpt.Rql.Services.Filtering.Operators;

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
