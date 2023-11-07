﻿using System.Reflection;
namespace SoftwareOne.Rql.Linq.Services.Filtering.Operators.Collection.Implementation;

internal interface ICollectionFunctions
{
    MethodInfo GetAnyWithPredicate();
    MethodInfo GetAnyWithNoPredicate();
    MethodInfo GetAll();
}

internal class CollectionFunctions<T> : ICollectionFunctions
{
    public MethodInfo GetAll()
    {
        Func<IEnumerable<T>, Func<T, bool>, bool> func = Enumerable.All;
        return func.Method;
    }

    public MethodInfo GetAnyWithNoPredicate()
    {
        Func<IEnumerable<T>, bool> func = Enumerable.Any;
        return func.Method;
    }

    public MethodInfo GetAnyWithPredicate()
    {
        Func<IEnumerable<T>, Func<T, bool>, bool> func = Enumerable.Any;
        return func.Method;
    }
}