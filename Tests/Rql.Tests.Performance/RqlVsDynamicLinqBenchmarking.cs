using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Rql.Tests.Performance.Factory;
using Rql.Tests.Performance.Model;
using SoftwareOne.Rql.Linq;
using System.Linq.Dynamic.Core;

namespace Rql.Tests.Performance;

[MemoryDiagnoser]
[ReturnValueValidator(failOnError: true)]
[MinColumn]
[MaxColumn]
public class RqlVsDynamicLinqBenchmarking
{
    private IHost _host;
    private IQueryable<SampleEntity> _query;

    [GlobalSetup]
    public void Setup()
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddSoftwareOneRql(t =>
                {
                    t.ScanForMappings(typeof(RqlVsDynamicLinqBenchmarking).Assembly);
                });
            })
            .Build();

        _query = new List<SampleEntity>().AsQueryable();
    }

    [Benchmark(Baseline = true)]
    public void RqlNet()
    {
        using var scope = _host.Services.CreateScope();
        var rql = scope.ServiceProvider.GetService<IRqlQueryable<SampleEntity>>()!;
        rql.Transform(_query, RqlRequestFactory.Rql);
    }

    [Benchmark]
    public void RqlNetLarge()
    {
        using var scope = _host.Services.CreateScope();
        var rql = scope.ServiceProvider.GetService<IRqlQueryable<SampleEntity>>()!;
        rql.Transform(_query, RqlRequestFactory.RqlLarge);
    }

    [Benchmark]
    public void DynamicLinq() => _query.Where(RqlRequestFactory.DynamicLinq.Filter!)
            .OrderBy(RqlRequestFactory.DynamicLinq.Order!)
            .Select(RqlRequestFactory.DynamicLinq.Select!);

    [Benchmark]
    public void DynamicLinqLarge() => _query.Where(RqlRequestFactory.DynamicLinqLarge.Filter!)
            .OrderBy(RqlRequestFactory.DynamicLinqLarge.Order!)
            .Select(RqlRequestFactory.DynamicLinqLarge.Select!);

    [GlobalCleanup]
    public void Cleanup() => _host.Dispose();
}