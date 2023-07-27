using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SoftwareOne.Rql;
using SoftwareOne.Rql.Linq;
using System.Linq.Dynamic.Core;

namespace Rql.Performance.Test
{
    [MemoryDiagnoser]
    [ReturnValueValidator(failOnError: true)]
    [MinColumn]
    [MaxColumn]
    public class RqlVsDynamicLinq
    {
        private IHost _host;
        private IQueryable<TestEntity> _query;
        private RqlRequest _requestRql;
        private RqlRequest _requestDl;

        [GlobalSetup]
        public void Setup()
        {
            _host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddSoftwareOneRql(t =>
                {
                    t.ScanForMappings(typeof(RqlVsDynamicLinq).Assembly);
                });
            })
            .Build();

            _requestRql = new RqlRequest
            {
                Filter = "and(eq(name,bobby),gt(id,3));date=123",
                Order = "-name,type",
                Select = "description"
            };

            _requestDl = new RqlRequest
            {
                Filter = "(Name = \"bobby\" AND Id > 3) OR Date = 123",
                Order = "Name desc, Type asc",
                Select = "new { Description }"
            };

            _query = new List<TestEntity>().AsQueryable();
        }

        [Benchmark(Baseline = true)]
        public void RqlNet()
        {
            using var scope = _host.Services.CreateScope();
            var rql = scope.ServiceProvider.GetService<IRqlQueryable<TestEntity>>()!;
            rql.Transform(_query, _requestRql);
        }

        [Benchmark]
        public void DynamicLinq() => _query.Where(_requestDl.Filter)
                .OrderBy(_requestDl.Order)
                .Select(_requestDl.Select);

        [GlobalCleanup]
        public void Cleanup() => _host.Dispose();
    }

    public class TestEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }
        public long Date { get; set; }
    }
}
