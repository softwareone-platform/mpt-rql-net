namespace Rql.Tests.Performance;

internal class Program
{
    static void Main(string[] args)
    {
        BenchmarkDotNet.Running.BenchmarkRunner.Run<RqlVsDynamicLinqBenchmarking>();
        Console.ReadKey();
    }
}
