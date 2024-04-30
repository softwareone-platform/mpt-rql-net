using SoftwareOne.Rql;

namespace Rql.Tests.Unit.Linq.Services.Models
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
