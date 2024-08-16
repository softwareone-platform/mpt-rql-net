namespace SoftwareOne.Rql.Linq.Configuration
{
    public class RqlStringFilterSettings
    {
        public StringComparisonType Type { get; set; } = StringComparisonType.Simple;
    }

    public enum StringComparisonType
    {
        Simple,
        Lexicographical
    }
}
