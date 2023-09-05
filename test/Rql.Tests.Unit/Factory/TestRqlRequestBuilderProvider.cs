using SoftwareOne.Rql.Client;
using SoftwareOne.Rql.Linq.Client;
using SoftwareOne.Rql.Linq.Client.RqlRequest;
using SoftwareOne.Rql.Linq.Core.Metadata;

namespace Rql.Tests.Unit.Factory;

internal class TestRqlRequestBuilderProvider : IRqlRequestBuilderProvider
{
    public IRqlRequestBuilder<T> GetRqlRequestBuilder<T>() where T : class
    {
        var propVisitor = new PropertyVisitor(new PropertyNameProvider());
        return new RqlRequestBuilder<T>(new RqlRequestGenerator(new FilterGenerator(propVisitor), new SelectGenerator(propVisitor),
            new OrderGenerator(propVisitor)));
    }
}