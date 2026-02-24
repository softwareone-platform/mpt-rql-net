using System.Reflection;

namespace Mpt.Rql.Services.Mapping;

internal interface IProjectionFunctions
{
    MethodInfo GetSelect();
    MethodInfo GetToList();
    MethodInfo GetFirstOrDefault();
}

internal class ProjectionFunctions<T> : ProjectionFunctions<T, T> { }

internal class ProjectionFunctions<TFrom, TTo> : IProjectionFunctions
{
    private static readonly MethodInfo _selectMethod;
    private static readonly MethodInfo _toListMethod;
    private static readonly MethodInfo _firstOrDefaultMethod;

    static ProjectionFunctions()
    {
        Func<IEnumerable<TFrom>, Func<TFrom, TTo>, IEnumerable<TTo>> selectFunc = Enumerable.Select;
        _selectMethod = selectFunc.Method;

        Func<IEnumerable<TTo>, List<TTo>> toListFunc = Enumerable.ToList;
        _toListMethod = toListFunc.Method;

        Func<IEnumerable<TTo>, TTo?> firstOrDefaultFunc = Enumerable.FirstOrDefault;
        _firstOrDefaultMethod = firstOrDefaultFunc.Method;
    }

    public MethodInfo GetSelect() => _selectMethod;

    public MethodInfo GetToList() => _toListMethod;

    public MethodInfo GetFirstOrDefault() => _firstOrDefaultMethod;
}