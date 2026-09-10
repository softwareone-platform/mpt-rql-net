using FluentAssertions;
using Mpt.Rql.Services.Ordering.Functions;
using System.Linq.Expressions;
using Xunit;

namespace Rql.Tests.Unit.Ordering;

public class CollectionValueMethodsTests
{
    [Fact]
    public void For_ReturnsClosedGenericEnumerableMethods()
    {
        var methods = CollectionValueMethods.For(typeof(string), typeof(int?));

        methods.Where.GetGenericArguments().Should().Equal(typeof(string));
        methods.Where.GetParameters().Should().HaveCount(2);
        methods.Where.GetParameters()[1].ParameterType.Should().Be(typeof(Func<string, bool>));

        methods.Select.GetGenericArguments().Should().Equal(typeof(string), typeof(int?));
        methods.Select.GetParameters()[1].ParameterType.Should().Be(typeof(Func<string, int?>));

        methods.FirstOrDefault.GetGenericArguments().Should().Equal(typeof(int?));
        methods.FirstOrDefault.GetParameters().Should().HaveCount(1);
    }

    [Fact]
    public void For_CachesOneInstancePerTypePair()
    {
        CollectionValueMethods.For(typeof(string), typeof(int))
            .Should().BeSameAs(CollectionValueMethods.For(typeof(string), typeof(int)));

        CollectionValueMethods.For(typeof(string), typeof(int))
            .Should().NotBeSameAs(CollectionValueMethods.For(typeof(string), typeof(long)));
    }

    [Fact]
    public void Methods_ComposeIntoAWorkingWhereSelectFirstOrDefaultChain()
    {
        var methods = CollectionValueMethods.For(typeof(string), typeof(int?));
        var source = Expression.Constant(new List<string> { "a", "bb", "ccc" });
        var element = Expression.Parameter(typeof(string), "e");
        var length = Expression.Property(element, nameof(string.Length));

        var where = Expression.Call(methods.Where, source,
            Expression.Lambda(Expression.GreaterThan(length, Expression.Constant(1)), element));
        var select = Expression.Call(methods.Select, where,
            Expression.Lambda(Expression.Convert(length, typeof(int?)), element));
        var first = Expression.Call(methods.FirstOrDefault, select);

        Expression.Lambda<Func<int?>>(first).Compile()().Should().Be(2);
    }
}
