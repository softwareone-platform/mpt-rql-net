using Mpt.Rql.Abstractions.Result;

namespace Mpt.Rql.Linq.Core;

public class Result<T>
{
    public T? Value { get; }
    public List<Error> Errors { get; }
    public bool IsError => Errors.Count > 0;

    public Result(T? value, List<Error>? errors = default)
    {
        Value = value;
        Errors = errors ?? new();
    }

    public static implicit operator Result<T>(T value)
    {
        return new Result<T>(value);
    }

    public static implicit operator Result<T>(Error error)
    {
        return new Result<T>(default, new() { error });
    }

    public static implicit operator Result<T>(List<Error> errors)
    {
        return new Result<T>(default, errors);
    }
}
