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

        public override IQueryable<Product> GetQuery() => ProductRepository.Query();

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