using System.Reflection;

namespace Mpt.Rql.Linq.Services.Mapping;

internal interface IProjectionFunctions
{
    MethodInfo GetSelect();
    MethodInfo GetToList();
}

internal class ProjectionFunctions<T> : ProjectionFunctions<T, T> { }

internal class ProjectionFunctions<TFrom, TTo> : IProjectionFunctions
{
    public MethodInfo GetSelect()
    {
        Func<IEnumerable<TFrom>, Func<TFrom, TTo>, IEnumerable<TTo>> func = Enumerable.Select;
        return func.Method;
    }

    public MethodInfo GetToList()
    {
        Func<IEnumerable<TTo>, List<TTo>> func = Enumerable.ToList;
        return func.Method;
    }
}