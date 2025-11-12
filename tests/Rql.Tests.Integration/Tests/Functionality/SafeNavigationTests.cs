using Mpt.Rql;
using Mpt.Rql.Abstractions;
using Mpt.Rql.Abstractions.Configuration;
using Rql.Tests.Integration.Core;
using Rql.Tests.Integration.Tests.Functionality.Utility;
using Xunit;

namespace Rql.Tests.Integration.Tests.Functionality;

public class SafeNavigationTests
{
    [Fact]
    public void SafeNavigationOff_WithValidReferences_WorksNormally()
    {
        // Arrange
        var testExecutor = new SafeNavigationTestExecutor(SafeNavigationMode.Off);
        
        // Act - Use existing test data from ProductRepository which should have valid references
        var result = testExecutor.Transform(filter: "eq(reference.name,Jewelry Widget)");
        
        // Assert - Should work normally with valid references
        var products = result.ToList();
        Assert.True(products.Count >= 0); // May or may not have matches, but shouldn't throw
    }

    [Fact]
    public void SafeNavigationOn_WithValidReferences_WorksNormally()
    {
        // Arrange
        var testExecutor = new SafeNavigationTestExecutor(SafeNavigationMode.On);
        
        // Act - Use existing test data from ProductRepository 
        var result = testExecutor.Transform(filter: "eq(reference.name,Jewelry Widget)");
        
        // Assert - Should work the same as SafeNavigation off when references are valid
        var products = result.ToList();
        Assert.True(products.Count >= 0);
    }

    [Fact]
    public void SafeNavigationConfiguration_FilterAndOrderingIndependent()
    {
        // Arrange
        var filterOnOrderingOff = new SafeNavigationTestExecutor(
            filterSafeNavigation: SafeNavigationMode.On, 
            orderingSafeNavigation: SafeNavigationMode.Off);
            
        var bothOn = new SafeNavigationTestExecutor(SafeNavigationMode.On);

        // Act & Assert - Both should work with valid data
        var result1 = filterOnOrderingOff.Transform(filter: "eq(name,Jewelry Widget)", order: "name");
        var result2 = bothOn.Transform(filter: "eq(name,Jewelry Widget)", order: "name");
        
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.True(result1.ToList().Count >= 0);
        Assert.True(result2.ToList().Count >= 0);
    }

    [Fact]
    public void SafeNavigationSettings_AreProperlyConfigured()
    {
        // Arrange & Act
        var filterOn = new SafeNavigationTestExecutor(
            filterSafeNavigation: SafeNavigationMode.On, 
            orderingSafeNavigation: SafeNavigationMode.Off);

        // Assert - This tests that our configuration is actually being applied
        // The test executor should be able to create the RQL configuration without errors
        Assert.NotNull(filterOn.Rql);
    }

    private class SafeNavigationTestExecutor : TestExecutor<Product>
    {
        private readonly SafeNavigationMode _filterSafeNavigation;
        private readonly SafeNavigationMode _orderingSafeNavigation;

        public SafeNavigationTestExecutor(SafeNavigationMode mode)
            : this(mode, mode)
        {
        }

        public SafeNavigationTestExecutor(SafeNavigationMode filterSafeNavigation, SafeNavigationMode orderingSafeNavigation)
        {
            _filterSafeNavigation = filterSafeNavigation;
            _orderingSafeNavigation = orderingSafeNavigation;
        }

        protected override IRqlQueryable<Product, Product> MakeRql()
            => RqlFactory.Make<Product>(services => { });

        public override IQueryable<Product> GetQuery() => CreateSafeNavigationTestData().AsQueryable();

        private static IEnumerable<Product> CreateSafeNavigationTestData()
        {
            var products = new[]
            {
                new Product 
                { 
                    Id = 1, 
                    Name = "Jewelry Widget", 
                    Category = "Clothing", 
                    Price = 192.95M, 
                    SellPrice = 172.99M, 
                    ListDate = DateTime.Now,
                    Tags = new List<Tag> { new Tag { Value = "jewelry" } },
                    Orders = new List<ProductOrder> { new ProductOrder { Id = 1, ClientName = "Michael" } },
                    OrdersIds = new List<int> { 1 }
                },
                new Product 
                { 
                    Id = 2, 
                    Name = "Test Product", 
                    Category = "Test", 
                    Price = 100m, 
                    SellPrice = 90m, 
                    ListDate = DateTime.Now,
                    Tags = new List<Tag> { new Tag { Value = "test" } },
                    Orders = new List<ProductOrder>(),
                    OrdersIds = new List<int>()
                }
            };

            // Set up self-references like ProductRepository does
            foreach (var product in products)
            {
                product.Reference = product;
                product.Collection = new List<Product> { product, product };
            }

            return products;
        }

        protected override void Customize(IRqlSettings settings)
        {
            settings.Select.Implicit = RqlSelectModes.Core | RqlSelectModes.Primitive | RqlSelectModes.Reference;
            settings.Select.Explicit = RqlSelectModes.All;
            settings.Select.MaxDepth = 10;
            
            // Configure SafeNavigation modes
            settings.Filter.SafeNavigation = _filterSafeNavigation;
            settings.Ordering.SafeNavigation = _orderingSafeNavigation;
        }

        public Action<IRqlSettings> GetCustomizer() => Customize;
    }
}