namespace Rql.Tests.Integration.Core
{
    public class Product : ITestEntity
    {
        public int Id { get; set; }
        public string? Desc { get; set; }
        public string Name { get; set; } = null!;
        public string Category { get; set; } = null!;
        public DateTime ListDate { get; set; }
        public decimal Price { get; set; }
        public decimal SellPrice { get; set; }
        public Product Sub { get; set; } = null!;
    }
}