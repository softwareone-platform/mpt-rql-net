namespace SoftwareOne.Rql.Linq.UnitTests.Services.Models
{
    internal class Item
    {
        [RqlProperty(IsCore = true)]
        public int Id { get; set; }

        [RqlProperty(IsCore = true)]
        public string Name { get; set; } = null!;

        public string Description { get; set; } = null!;
    }
}
