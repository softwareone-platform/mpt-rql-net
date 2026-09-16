namespace Mpt.Rql.Services.Ordering.Functions;

/// <summary>Error codes raised by ordering functions. Same <c>order:&lt;sub&gt;</c> shape as <c>RqlService.MakeErrorCode</c>.</summary>
internal static class OrderingErrorCodes
{
    public const string UnknownFunction = "order:unknown_func";
    public const string FunctionArguments = "order:func_args";
    public const string NotCollection = "order:not_collection";
    public const string NotPrimitive = "order:not_primitive";
}
