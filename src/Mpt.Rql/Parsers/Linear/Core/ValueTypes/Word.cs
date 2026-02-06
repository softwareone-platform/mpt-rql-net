using Mpt.Rql.Parsers.Linear.Core.Enumerations;

namespace Mpt.Rql.Parsers.Linear.Core.ValueTypes;

internal struct Word
{
    public ReadOnlyMemory<char> Text { get; set; }
    public int WordStart { get; set; }
    public int WordLength { get; set; }

    public char? QuoteSymbol { get; set; }
    public int? QuoteStart { get; set; }
    public int? QuoteEnd { get; set; }
    public GroupType GroupType { get; set; }
    public bool IsQuoted { get; set; }

    public override readonly string ToString() => Text.Slice(WordStart, WordLength).ToString();

    public static Word Make(ReadOnlyMemory<char> text, int start)
        => new() { Text = text, WordStart = start };
}
