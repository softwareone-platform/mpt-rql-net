namespace Mpt.Rql.Abstractions.Result;

public class Error
{
    public ErrorType Type { get; }
    public string Code { get; }
    public string? Path { get; }
    public string Message { get; }

    private Error(ErrorType type, string code, string? path, string message)
    {
        Type = type;
        Code = code;
        Path = path;
        Message = message;
    }

    public static Error Validation(string message, string? code = null, string? path = null)
    {
        return new Error(ErrorType.Validation, code ?? "rql_validation", path, message);
    }

    public static Error General(string message, string? code = null, string? path = null)
    {
        return new Error(ErrorType.General, code ?? "rql_failure", path, message);
    }

    public override string ToString()
    {
        var result = $"{Type}: {Code} - {Message}";
        if (!string.IsNullOrEmpty(Path))
            result += $"(Path: {Path})";

        return result;
    }
}
