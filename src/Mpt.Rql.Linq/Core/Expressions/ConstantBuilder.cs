using System.Linq.Expressions;
using System.Reflection;

namespace Mpt.Rql.Linq.Core.Expressions;

internal static class ConstantBuilder
{
    public static Expression Build(object? value, Type type)
    {
        var actualType = typeof(HostedConstant<>).MakeGenericType(type);
        var instance = (IHostedConstant)Activator.CreateInstance(actualType)!;
        instance.SetValue(value);
        return Expression.Property(Expression.Constant(instance, actualType), "Value");
    }
}

internal interface IHostedConstant
{
    void SetValue(object? value);
}

internal class HostedConstant<T> : IHostedConstant
{
    public T? Value { get; private set; }

    public void SetValue(object? value)
    {
        Value = (T?)value;
    }
}
