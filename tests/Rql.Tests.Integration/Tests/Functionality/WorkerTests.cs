using Mpt.Rql;
using Mpt.Rql.Abstractions.Configuration;
using Rql.Tests.Integration.Core;
using Xunit;

namespace Rql.Tests.Integration.Tests.Functionality;

public class WorkerTests
{
    private readonly IRqlQueryable<Product, Product> _rql;

    public WorkerTests()
    {
        _rql = RqlFactory.MakeWorker<Product>(services => { }, rql =>
        {
            rql.Settings.Select.Implicit = RqlSelectModes.All;
            rql.Settings.Select.Explicit = RqlSelectModes.All;
            rql.Settings.Select.MaxDepth = 3;
            rql.Settings.Filter.Navigation = NavigationStrategy.Safe;
            rql.Settings.Ordering.Navigation = NavigationStrategy.Safe;
            rql.Settings.Mapping.Navigation = NavigationStrategy.Safe;
        });
    }

    [Theory]
    [InlineData("eq(name,Jewelry Widget)", 1)]
    [InlineData("eq(category,Activity)", 3)]
    [InlineData("gt(price,200)", 3)]
    [InlineData("and(eq(category,Activity),gt(price,100))", 2)]
    public void Filter_ReturnsExpectedCount(string filter, int expectedCount)
    {
        var testData = ProductRepository.Query();

        var result = _rql.Transform(testData, new RqlRequest { Filter = filter });

        Assert.True(result.IsSuccess);
        Assert.Equal(expectedCount, result.Query.Count());
    }

    [Theory]
    [InlineData("+category,+id")]
    [InlineData("-price")]
    public void Order_AppliesCorrectly(string order)
    {
        var testData = ProductRepository.Query();

        var result = _rql.Transform(testData, new RqlRequest { Order = order });

        Assert.True(result.IsSuccess);
        Assert.Equal(8, result.Query.Count());
    }

    [Fact]
    public void Order_Ascending_VerifySequence()
    {
        var testData = ProductRepository.Query();

        var result = _rql.Transform(testData, new RqlRequest { Order = "+price" });

        Assert.True(result.IsSuccess);
        var products = result.Query.ToList();
        for (int i = 1; i < products.Count; i++)
        {
            Assert.True(products[i].Price >= products[i - 1].Price);
        }
    }

    [Fact]
    public void BuildGraph_ReturnsGraph()
    {
        var result = _rql.BuildGraph(new RqlRequest { Filter = "eq(name,Test)", Select = "id,name" });

        Assert.NotNull(result.Graph);
    }

    [Fact]
    public void Filter_InvalidProperty_ReturnsError()
    {
        var testData = ProductRepository.Query();

        var result = _rql.Transform(testData, new RqlRequest { Filter = "eq(nonexistent,value)" });

        Assert.False(result.IsSuccess);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void SequentialCalls_StateResetsCorrectly()
    {
        var testData = ProductRepository.Query();

        // First call: filter
        var result1 = _rql.Transform(testData, new RqlRequest { Filter = "eq(category,Activity)" });
        Assert.True(result1.IsSuccess);
        Assert.Equal(3, result1.Query.Count());

        // Second call: different filter — must not carry over state from first call
        var result2 = _rql.Transform(testData, new RqlRequest { Filter = "eq(category,Home)" });
        Assert.True(result2.IsSuccess);
        Assert.Equal(1, result2.Query.Count());

        // Third call: no filter — must return all
        var result3 = _rql.Transform(testData, new RqlRequest());
        Assert.True(result3.IsSuccess);
        Assert.Equal(8, result3.Query.Count());
    }

    [Fact]
    public void SequentialCalls_ErrorDoesNotLeakIntoNextCall()
    {
        var testData = ProductRepository.Query();

        // First call: intentional error
        var errorResult = _rql.Transform(testData, new RqlRequest { Filter = "eq(nonexistent,value)" });
        Assert.False(errorResult.IsSuccess);

        // Second call: valid — must succeed without leftover errors
        var successResult = _rql.Transform(testData, new RqlRequest { Filter = "eq(category,Activity)" });
        Assert.True(successResult.IsSuccess);
        Assert.Equal(3, successResult.Query.Count());
    }

    [Fact]
    public void SequentialCalls_OrderDoesNotLeakIntoNextCall()
    {
        var testData = ProductRepository.Query();

        // First call: ordered ascending
        var result1 = _rql.Transform(testData, new RqlRequest { Order = "+price" });
        Assert.True(result1.IsSuccess);
        var first1 = result1.Query.First();

        // Second call: ordered descending — must reflect new order
        var result2 = _rql.Transform(testData, new RqlRequest { Order = "-price" });
        Assert.True(result2.IsSuccess);
        var first2 = result2.Query.First();

        Assert.NotEqual(first1.Id, first2.Id);
    }

    [Fact]
    public void ConcurrentCalls_ThreadSafety()
    {
        const int threadCount = 10;
        const int callsPerThread = 20;
        var exceptions = new List<Exception>();

        var threads = Enumerable.Range(0, threadCount).Select(t => new Thread(() =>
        {
            try
            {
                for (int i = 0; i < callsPerThread; i++)
                {
                    var testData = ProductRepository.Query();
                    var result = _rql.Transform(testData, new RqlRequest { Filter = "eq(category,Activity)" });

                    Assert.True(result.IsSuccess);
                    Assert.Equal(3, result.Query.Count());
                }
            }
            catch (Exception ex)
            {
                lock (exceptions)
                {
                    exceptions.Add(ex);
                }
            }
        })).ToList();

        threads.ForEach(t => t.Start());
        threads.ForEach(t => t.Join());

        Assert.Empty(exceptions);
    }

    [Fact]
    public void ConcurrentCalls_DifferentFilters()
    {
        var filters = new (string filter, int expected)[]
        {
            ("eq(category,Activity)", 3),
            ("eq(category,Home)", 1),
            ("eq(category,Clothing)", 1),
            ("gt(price,200)", 3),
            ("eq(category,Beauty)", 1),
        };

        var exceptions = new List<Exception>();
        var threads = filters.Select(f => new Thread(() =>
        {
            try
            {
                for (int i = 0; i < 20; i++)
                {
                    var testData = ProductRepository.Query();
                    var result = _rql.Transform(testData, new RqlRequest { Filter = f.filter });

                    Assert.True(result.IsSuccess);
                    Assert.Equal(f.expected, result.Query.Count());
                }
            }
            catch (Exception ex)
            {
                lock (exceptions)
                {
                    exceptions.Add(ex);
                }
            }
        })).ToList();

        threads.ForEach(t => t.Start());
        threads.ForEach(t => t.Join());

        Assert.Empty(exceptions);
    }

    [Fact]
    public void FilterAndOrder_Combined()
    {
        var testData = ProductRepository.Query();

        var result = _rql.Transform(testData, new RqlRequest
        {
            Filter = "eq(category,Activity)",
            Order = "+price"
        });

        Assert.True(result.IsSuccess);
        var products = result.Query.ToList();
        Assert.Equal(3, products.Count);
        for (int i = 1; i < products.Count; i++)
        {
            Assert.True(products[i].Price >= products[i - 1].Price);
        }
    }

    [Fact]
    public void EmptyRequest_ReturnsAll()
    {
        var testData = ProductRepository.Query();

        var result = _rql.Transform(testData, new RqlRequest());

        Assert.True(result.IsSuccess);
        Assert.Equal(8, result.Query.Count());
    }
}
