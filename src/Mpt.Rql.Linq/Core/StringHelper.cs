namespace Mpt.Rql.Linq.Core;

internal static class StringHelper
{
    private static readonly Dictionary<char, bool> _signMap = new()
    {
        { '+', true },
        { ' ', true },
        { '-', false }
    };

    public static (ReadOnlyMemory<char> value, bool sign) ExtractSign(string initialValue)
    {
        var sign = initialValue.Length > 0 ? initialValue[0] : (char?)null;

        if (sign.HasValue && _signMap.TryGetValue(sign.Value, out var mod))
        {
            return (initialValue.AsMemory(1), mod);
        }

        return (initialValue.AsMemory(), true);
    }
}
