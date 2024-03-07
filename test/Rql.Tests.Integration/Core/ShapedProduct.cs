using SoftwareOne.Rql;

namespace Rql.Tests.Integration.Core
{
    public class ShapedProduct : ITestEntity
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!;

        public decimal Price { get; set; }

        public string Category { get; set; } = null!;

        public DateTime ListDate { get; set; }

        [RqlProperty(Select = RqlSelectModes.All)]
        public ShapedProductReference? Reference { get; set; } = null!;

        [RqlProperty(Select = RqlSelectModes.All)]
        public List<ShapedProductReference> Collection { get; set; } = null!;

        [RqlProperty(Select = RqlSelectModes.None)]
        public List<ShapedProductReference> HiddenCollection { get; set; } = null!;

        [RqlProperty(IsIgnored = true)]
        public ShapedProductReference? Ignored { get; set; } = null!;
    }

    public class ShapedProductReference
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!;
    }
}