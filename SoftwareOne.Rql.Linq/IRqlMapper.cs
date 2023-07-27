using System.Linq.Expressions;

#pragma warning disable IDE0130
namespace SoftwareOne.Rql
{
    public interface IRqlMapper
    {
    }

    public interface IRqlMapper<TStorage, TView> : IRqlMapper
    {
        public Expression<Func<TStorage, TView>> GetMapping();
    }
}
