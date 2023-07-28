using System.Reflection;

namespace SoftwareOne.Rql.Linq.Services.Projection
{
    internal interface IProjectionFunctions
    {
        MethodInfo GetSelect();
        MethodInfo GetToList();
    }

    internal class ProjectionFunctions<T> : IProjectionFunctions
    {
        public MethodInfo GetSelect()
        {
            Func<IEnumerable<T>, Func<T, T>, IEnumerable<T>> func = Enumerable.Select;
            return func.Method;
        }

        public MethodInfo GetToList()
        {
            Func<IEnumerable<T>, List<T>> func = Enumerable.ToList;
            return func.Method;
        }
    }

}
