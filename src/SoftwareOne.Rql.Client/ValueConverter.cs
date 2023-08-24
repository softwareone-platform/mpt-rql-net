using System.Globalization;

namespace SoftwareOne.Rql.Client;

internal static class ValueConverter
{
    private const string NullConst = "null()";
    private const string EmptyConst = "empty()";

    private static readonly IList<Type> NumericTypes = new List<Type>
    {
        typeof(decimal),
        typeof(double),
        typeof(short),
        typeof(int),
        typeof(long),
        typeof(float),
    };

    public static string Convert<U>(U? value)
    {
        var valueFormatted = value switch
        {
            null => NullConst,
            string v when string.IsNullOrWhiteSpace(v) => EmptyConst,
            string v => $"'{v}'",
            char v => $"'{v}'",
            var v when NumericTypes.Contains(v.GetType()) => ((IConvertible)v).ToString(CultureInfo
                .InvariantCulture), // TODO: switch to INumber<> implementation https://learn.microsoft.com/en-us/dotnet/api/system.numerics.inumber-1?view=net-7.0
            DateTime v => v.ToString("o"),
            DateTimeOffset v => v.ToString("o"),
            bool v => v.ToString().ToLowerInvariant(),
            Enum e => e.ToString("D"),
            _ => value.ToString()
        };

        return valueFormatted;
    }
}