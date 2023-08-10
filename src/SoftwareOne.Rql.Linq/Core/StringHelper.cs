namespace SoftwareOne.Rql.Linq.Core;

internal class StringHelper
{
    private static readonly Dictionary<char, bool> _signMap;

    static StringHelper()
    {
        _signMap = new Dictionary<char, bool>()
        {
            { '+', true },
            { ' ', true },
            { '-', false }
        };
    }

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
