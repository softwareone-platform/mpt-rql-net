using System.Globalization;
using ErrorOr;

namespace SoftwareOne.Rql.Linq.Services.Filtering.Operators
{
    internal static class ConstantHelper
    {
        public static ErrorOr<object> ChangeType(string value, Type type)
        {
            try
            {
                return Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
            }
            catch (FormatException)
            {
                return Error.Validation(description: $"Incorrect input format: '{value}'.");
            }
            catch
            {
                return Error.Validation(description: $"Error converting constant.");
            }
        }
    }
}
