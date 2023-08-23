using SoftwareOne.Rql.Parsers.Linear.Domain.Core.Enumerations;

namespace SoftwareOne.Rql.Parsers.Linear.Domain.Core.ValueTypes
{
    internal struct Word
    {
        public ReadOnlyMemory<char> Text { get; set; }
        public int WordStart { get; set; }
        public int WordLength { get; set; }

        public char? WrapSymbol { get; set; }
        public int? WrapStart { get; set; }
        public int? WrapEnd { get; set; }
        public GroupType GroupType { get; set; }
        public List<int> Delimiters { get; set; }

        public readonly override string ToString() => Text.Slice(WordStart, WordLength).ToString();

        public static Word Make(ReadOnlyMemory<char> text, int start)
            => new() { Text = text, WordStart = start, Delimiters = new List<int>() };
    }
}
