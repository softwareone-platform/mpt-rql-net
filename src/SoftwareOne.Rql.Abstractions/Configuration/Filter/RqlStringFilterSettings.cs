namespace SoftwareOne.Rql.Abstractions.Configuration.Filter;

public class RqlStringFilterSettings
{
    public StringComparisonType Type { get; set; } = StringComparisonType.Simple;
}

public enum StringComparisonType
{
    Simple,
    Lexicographical
}
