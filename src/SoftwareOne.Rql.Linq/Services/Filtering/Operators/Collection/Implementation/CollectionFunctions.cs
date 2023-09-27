using System.Reflection;
namespace SoftwareOne.Rql.Linq.Services.Filtering.Operators.Collection.Implementation;

internal interface ICollectionFunctions
{
    MethodInfo GetAny();
    MethodInfo GetAll();
}

internal class CollectionFunctions<T> : ICollectionFunctions
{
    public MethodInfo GetAll()
    {
        Func<IEnumerable<T>, Func<T, bool>, bool> func = Enumerable.All;
        return func.Method;
    }

    public MethodInfo GetAny()
    {
        Func<IEnumerable<T>, Func<T, bool>, bool> func = Enumerable.Any;
        return func.Method;
    }
}