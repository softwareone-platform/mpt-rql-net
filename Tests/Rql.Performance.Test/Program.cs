namespace Rql.Performance.Test
{
    internal class Program
    {
        static void Main(string[] args)
        {
            BenchmarkDotNet.Running.BenchmarkRunner.Run<RqlVsDynamicLinq>();
            Console.ReadKey();
        }
    }
}