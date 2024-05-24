namespace SoftwareOne.Rql.Linq.Core.Result
{
    public class Error
    {
        public ErrorType Type { get; }
        public string Code { get; }
        public string Message { get; }

        private Error(ErrorType type, string code, string message)
        {
            Type = type;
            Code = code;
            Message = message;
        }

        public static Error Validation(string message, string? code = null)
        {
            return new Error(ErrorType.Validation, code ?? "rql_validation", message);
        }

        public static Error General(string message, string? code = null)
        {
            return new Error(ErrorType.General, code ?? "rql_failure", message);
        }

        public override string ToString()
        {
            return $"{Type}: {Code} - {Message}";
        }
    }
}
