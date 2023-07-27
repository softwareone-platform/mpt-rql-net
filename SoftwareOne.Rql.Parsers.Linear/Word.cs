namespace SoftwareOne.Rql.Parsers.Linear
{
    internal struct Word
    {
        public ReadOnlyMemory<char> Text { get; set; }
        public int WordStart { get; set; }
        public int WordLenght { get; set; }

        public char? WrapSymbol { get; set; }
        public int? WrapStart { get; set; }
        public int? WrapEnd { get; set; }
        public GroupType GroupType { get; set; }
        public List<int> Delimeters { get; set; }

        public override readonly string ToString() => Text.Slice(WordStart, WordLenght).ToString();

        public static Word Make(ReadOnlyMemory<char> text, int start)
            => new() { Text = text, WordStart = start, Delimeters = new List<int>() };
    }
}
