using Microsoft.Extensions.DependencyInjection;
using SoftwareOne.Rql.Client;
using SoftwareOne.Rql.Linq.Client.Request;

namespace SoftwareOne.Rql.Linq.Client.Builder.Request;

internal class RqlRequestBuilder<T> : IRqlRequestBuilder<T> where T : class
{
    private readonly IServiceProvider _serviceProvider;
    public RqlRequestBuilder(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Rql.RqlRequest Build() => new();

    public IFilteredRqlRequestBuilder<T> Where(Func<IFilterContext<T>, IOperator> filter)
        => MakeContext().Where(filter);

    public IRqlRequestBuilder Select(Func<ISelectContext<T>, ISelectContext<T>> select)
        => MakeContext().Select(select);

    public IOrderedRqlRequestBuilder<T> OrderBy<TValue>(System.Linq.Expressions.Expression<Func<T, TValue>> order)
        => MakeContext().OrderBy(order);

    public IOrderedRqlRequestBuilder<T> OrderByDescending<TValue>(System.Linq.Expressions.Expression<Func<T, TValue>> order)
        => MakeContext().OrderByDescending(order);

    private IRqlRequestBuilder<T> MakeContext() => (IRqlRequestBuilder<T>)_serviceProvider.GetRequiredService(typeof(IRqlRequestBuilderContext<T>));
}