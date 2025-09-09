using FluentAssertions;
using Moq;
using SoftwareOne.Rql.Abstractions;
using SoftwareOne.Rql.Abstractions.Configuration;
using SoftwareOne.Rql.Linq.Configuration;
using SoftwareOne.Rql.Linq.Core;
using SoftwareOne.Rql.Linq.Core.Metadata;
using SoftwareOne.Rql.Linq.Services.Context;
using SoftwareOne.Rql.Linq.Services.Filtering;
using SoftwareOne.Rql.Linq.Services.Ordering;
using SoftwareOne.Rql.Linq.Services.Projection;
using SoftwareOne.Rql.Linq.UnitTests.Services.Models;
using SoftwareOne.Rql.Parsers.Linear.Domain.Services;
using Xunit;

namespace SoftwareOne.Rql.Linq.UnitTests.Services;

public class GraphBuilderTests
{
    private readonly FilteringGraphBuilder<Product> _filteringBuilder;
    private readonly OrderingGraphBuilder<Product> _orderingBuilder;
    private readonly ProjectionGraphBuilder<Product> _projectionBuilder;
    private readonly IQueryContext<Product> _queryContext;
    private readonly RqlParser _rqlParser;

    public GraphBuilderTests()
    {
        var actionValidatorMock = new Mock<IActionValidator>();
        actionValidatorMock.Setup(av => av.Validate(It.IsAny<RqlPropertyInfo>(), It.IsAny<RqlActions>())).Returns(true);

        _queryContext = new QueryContext<Product>();
        _rqlParser = new RqlParser();

        var settings = new GlobalRqlSettings { Select = new RqlSelectSettings { Explicit = RqlSelectModes.All, Implicit = RqlSelectModes.Core | RqlSelectModes.Primitive } };
        var metadataProvider = new MetadataProvider(new PropertyNameProvider(), new MetadataFactory(settings));
        var builderContext = new BuilderContext();

        _projectionBuilder = new ProjectionGraphBuilder<Product>(_queryContext, metadataProvider, actionValidatorMock.Object, builderContext, settings);
        _filteringBuilder = new FilteringGraphBuilder<Product>(metadataProvider, actionValidatorMock.Object, builderContext);
        _orderingBuilder = new OrderingGraphBuilder<Product>(metadataProvider, actionValidatorMock.Object, builderContext);
    }

    [Fact]
    public void TraverseRqlExpression_WithEmptyExpression_BuildsDefaultGraph()
        => RunTest(string.Empty, string.Empty, string.Empty);
    
    [Fact]
    public void TraverseRqlExpression_WhenHidingProperty_propertyIsHidden()
        => RunTest(string.Empty, string.Empty, "-coreCategory", CoreCategoryHidden);
    
    [Fact]
    public void TraverseRqlExpression_WhenSelectingAndHidingProperty_propertyIsSelectedAndHidden()
        => RunTest(string.Empty, string.Empty, "coreCategory,-coreCategory", CoreCategorySelectedAndHidden);
    
    [Fact]
    public void TraverseRqlExpression_WhenHidingHiddenProperty_propertyIsHiddenForTwoReasons()
        => RunTest(string.Empty, string.Empty, "-hiddenCategory", HiddenCategoryHidden);
    
    [Fact]
    public void TraverseRqlExpression_WhenSelectingAndHidingHiddenProperty_propertyIsSelectedAndHiddenForTwoReasons()
        => RunTest(string.Empty, string.Empty, "hiddenCategory,-hiddenCategory", HiddenCategorySelectedAndHidden);
    
    [Fact]
    public void TraverseRqlExpression_WhenHidingAllProperties_propertiesAreHidden()
        => RunTest(string.Empty, string.Empty, "-*", p =>
        {
            p.Property("id", IncludeReasons.Default, ExcludeReasons.Unselected);
            p.Property("name", IncludeReasons.Default, ExcludeReasons.Unselected);
            p.Property("description", IncludeReasons.Default, ExcludeReasons.Unselected);
            
            p.Property("category", IncludeReasons.Default, ExcludeReasons.Unselected);
            p.Property("category.description", IncludeReasons.None, ExcludeReasons.Default);
            p.Property("category.id", IncludeReasons.None, ExcludeReasons.Default);
            p.Property("category.name", IncludeReasons.None, ExcludeReasons.Default);
            p.Property("category.products", IncludeReasons.None, ExcludeReasons.Default);

            CoreCategoryHidden(p);
            
            p.Property("ignoredCategory", IncludeReasons.None, ExcludeReasons.Unselected);
            p.Property("ignoredCategory.description", IncludeReasons.None, ExcludeReasons.Default);
            p.Property("ignoredCategory.id", IncludeReasons.None, ExcludeReasons.Default);
            p.Property("ignoredCategory.name", IncludeReasons.None, ExcludeReasons.Default);
            p.Property("ignoredCategory.products", IncludeReasons.None, ExcludeReasons.Default);

            HiddenCategoryHidden(p);
            
            p.Property("items", IncludeReasons.Default, ExcludeReasons.Unselected);
            p.Property("items.description", IncludeReasons.None, ExcludeReasons.Default);
            p.Property("items.id", IncludeReasons.None, ExcludeReasons.Default);
            p.Property("items.name", IncludeReasons.None, ExcludeReasons.Default);
            
            p.Property("coreItems", IncludeReasons.Default, ExcludeReasons.Unselected);
            p.Property("coreItems.description", IncludeReasons.None, ExcludeReasons.Default);
            p.Property("coreItems.id", IncludeReasons.None, ExcludeReasons.Default);
            p.Property("coreItems.name", IncludeReasons.None, ExcludeReasons.Default);
        });

    [Fact]
    public void TraverseRqlExpression_WhenSelectingAllProperties_propertiesAreSelected()
        => RunTest(string.Empty, string.Empty, "*", p =>
        {
            p.Property("id", IncludeReasons.Default | IncludeReasons.Select, ExcludeReasons.None);
            p.Property("name", IncludeReasons.Default | IncludeReasons.Select, ExcludeReasons.None);
            p.Property("description", IncludeReasons.Default | IncludeReasons.Select, ExcludeReasons.None);
            p.Property("coreItems", IncludeReasons.Default | IncludeReasons.Select, ExcludeReasons.None);
            p.Property("items", IncludeReasons.Default | IncludeReasons.Select, ExcludeReasons.None);
            p.Property("category", IncludeReasons.Default | IncludeReasons.Select, ExcludeReasons.None);
            p.Property("category.products", IncludeReasons.Default, ExcludeReasons.None);
            
            p.Property("category.products.category", IncludeReasons.None, ExcludeReasons.Default);
            p.Property("category.products.coreCategory", IncludeReasons.Default, ExcludeReasons.None);
            p.Property("category.products.coreCategory.description", IncludeReasons.Default, ExcludeReasons.None);
            p.Property("category.products.coreCategory.id", IncludeReasons.Default, ExcludeReasons.None);
            p.Property("category.products.coreCategory.name", IncludeReasons.Default, ExcludeReasons.None);
            p.Property("category.products.coreCategory.products", IncludeReasons.None, ExcludeReasons.Default);
            p.Property("category.products.coreItems", IncludeReasons.Default, ExcludeReasons.None);
            p.Property("category.products.coreItems.description", IncludeReasons.Default, ExcludeReasons.None);
            p.Property("category.products.coreItems.id", IncludeReasons.Default, ExcludeReasons.None);
            p.Property("category.products.coreItems.name", IncludeReasons.Default, ExcludeReasons.None);
            p.Property("category.products.description", IncludeReasons.Default, ExcludeReasons.None);
            p.Property("category.products.hiddenCategory", IncludeReasons.None, ExcludeReasons.Default);
            p.Property("category.products.id", IncludeReasons.Default, ExcludeReasons.None);
            p.Property("category.products.items", IncludeReasons.None, ExcludeReasons.Default);
            p.Property("category.products.name", IncludeReasons.Default, ExcludeReasons.None);
            
            p.Property("coreCategory", IncludeReasons.Default | IncludeReasons.Select, ExcludeReasons.None);
            p.Property("coreCategory.products", IncludeReasons.Default, ExcludeReasons.None);
            
            p.Property("coreCategory.products.category", IncludeReasons.None, ExcludeReasons.Default);
            p.Property("coreCategory.products.coreCategory", IncludeReasons.Default, ExcludeReasons.None);
            p.Property("coreCategory.products.coreCategory.description", IncludeReasons.Default, ExcludeReasons.None);
            p.Property("coreCategory.products.coreCategory.id", IncludeReasons.Default, ExcludeReasons.None);
            p.Property("coreCategory.products.coreCategory.name", IncludeReasons.Default, ExcludeReasons.None);
            p.Property("coreCategory.products.coreCategory.products", IncludeReasons.None, ExcludeReasons.Default);
            p.Property("coreCategory.products.coreItems", IncludeReasons.Default, ExcludeReasons.None);
            p.Property("coreCategory.products.coreItems.description", IncludeReasons.Default, ExcludeReasons.None);
            p.Property("coreCategory.products.coreItems.id", IncludeReasons.Default, ExcludeReasons.None);
            p.Property("coreCategory.products.coreItems.name", IncludeReasons.Default, ExcludeReasons.None);
            p.Property("coreCategory.products.description", IncludeReasons.Default, ExcludeReasons.None);
            p.Property("coreCategory.products.hiddenCategory", IncludeReasons.None, ExcludeReasons.Default);
            p.Property("coreCategory.products.id", IncludeReasons.Default, ExcludeReasons.None);
            p.Property("coreCategory.products.items", IncludeReasons.None, ExcludeReasons.Default);
            p.Property("coreCategory.products.name", IncludeReasons.Default, ExcludeReasons.None);

            p.Property("ignoredCategory", IncludeReasons.Select, ExcludeReasons.None);
            p.Property("ignoredCategory.description", IncludeReasons.Default, ExcludeReasons.None);
            p.Property("ignoredCategory.id", IncludeReasons.Default, ExcludeReasons.None);
            p.Property("ignoredCategory.name", IncludeReasons.Default, ExcludeReasons.None);
            p.Property("ignoredCategory.products", IncludeReasons.Default, ExcludeReasons.None);
            
            p.Property("ignoredCategory.products.category", IncludeReasons.None, ExcludeReasons.Default);
            p.Property("ignoredCategory.products.coreCategory", IncludeReasons.Default, ExcludeReasons.None);
            p.Property("ignoredCategory.products.coreCategory.description", IncludeReasons.Default, ExcludeReasons.None);
            p.Property("ignoredCategory.products.coreCategory.id", IncludeReasons.Default, ExcludeReasons.None);
            p.Property("ignoredCategory.products.coreCategory.name", IncludeReasons.Default, ExcludeReasons.None);
            p.Property("ignoredCategory.products.coreCategory.products", IncludeReasons.None, ExcludeReasons.Default);
            p.Property("ignoredCategory.products.coreItems", IncludeReasons.Default, ExcludeReasons.None);
            p.Property("ignoredCategory.products.coreItems.description", IncludeReasons.Default, ExcludeReasons.None);
            p.Property("ignoredCategory.products.coreItems.id", IncludeReasons.Default, ExcludeReasons.None);
            p.Property("ignoredCategory.products.coreItems.name", IncludeReasons.Default, ExcludeReasons.None);
            p.Property("ignoredCategory.products.description", IncludeReasons.Default, ExcludeReasons.None);
            p.Property("ignoredCategory.products.hiddenCategory", IncludeReasons.None, ExcludeReasons.Default);
            p.Property("ignoredCategory.products.id", IncludeReasons.Default, ExcludeReasons.None);
            p.Property("ignoredCategory.products.items", IncludeReasons.None, ExcludeReasons.Default);
            p.Property("ignoredCategory.products.name", IncludeReasons.Default, ExcludeReasons.None);

            p.Property("hiddenCategory",  IncludeReasons.Select, ExcludeReasons.Default);
            p.Property("hiddenCategory.description", IncludeReasons.Default, ExcludeReasons.None);
            p.Property("hiddenCategory.id", IncludeReasons.Default, ExcludeReasons.None);
            p.Property("hiddenCategory.name", IncludeReasons.Default, ExcludeReasons.None);
            p.Property("hiddenCategory.products", IncludeReasons.Default, ExcludeReasons.None);
            
            p.Property("hiddenCategory.products.category", IncludeReasons.None, ExcludeReasons.Default);
            p.Property("hiddenCategory.products.coreCategory", IncludeReasons.Default, ExcludeReasons.None);
            p.Property("hiddenCategory.products.coreCategory.description", IncludeReasons.Default, ExcludeReasons.None);
            p.Property("hiddenCategory.products.coreCategory.id", IncludeReasons.Default, ExcludeReasons.None);
            p.Property("hiddenCategory.products.coreCategory.name", IncludeReasons.Default, ExcludeReasons.None);
            p.Property("hiddenCategory.products.coreCategory.products", IncludeReasons.None, ExcludeReasons.Default);
            p.Property("hiddenCategory.products.coreItems", IncludeReasons.Default, ExcludeReasons.None);
            p.Property("hiddenCategory.products.coreItems.description", IncludeReasons.Default, ExcludeReasons.None);
            p.Property("hiddenCategory.products.coreItems.id", IncludeReasons.Default, ExcludeReasons.None);
            p.Property("hiddenCategory.products.coreItems.name", IncludeReasons.Default, ExcludeReasons.None);
            p.Property("hiddenCategory.products.description", IncludeReasons.Default, ExcludeReasons.None);
            p.Property("hiddenCategory.products.hiddenCategory", IncludeReasons.None, ExcludeReasons.Default);
            p.Property("hiddenCategory.products.id", IncludeReasons.Default, ExcludeReasons.None);
            p.Property("hiddenCategory.products.items", IncludeReasons.None, ExcludeReasons.Default);
            p.Property("hiddenCategory.products.name", IncludeReasons.Default, ExcludeReasons.None);
        });
    
    [Fact]
    public void TraverseRqlExpression_WhenSelectingAndHidingAllProperties_propertiesAreSelectedAndHidden()
        => RunTest(string.Empty, string.Empty, "*,-*", p =>
        {
            p.Property("id", IncludeReasons.Default | IncludeReasons.Select, ExcludeReasons.Unselected);
            p.Property("name", IncludeReasons.Default | IncludeReasons.Select, ExcludeReasons.Unselected);
            p.Property("description", IncludeReasons.Default | IncludeReasons.Select, ExcludeReasons.Unselected);
            p.Property("coreItems", IncludeReasons.Default | IncludeReasons.Select, ExcludeReasons.Unselected);
            p.Property("items", IncludeReasons.Default | IncludeReasons.Select, ExcludeReasons.Unselected);
            
            p.Property("category", IncludeReasons.Default | IncludeReasons.Select, ExcludeReasons.Unselected);
            p.Property("category.products", IncludeReasons.Default, ExcludeReasons.None);
            
            p.Property("category.products.category", IncludeReasons.None, ExcludeReasons.Default);
            p.Property("category.products.coreCategory", IncludeReasons.Default, ExcludeReasons.None);
            p.Property("category.products.coreCategory.description", IncludeReasons.Default, ExcludeReasons.None);
            p.Property("category.products.coreCategory.id", IncludeReasons.Default, ExcludeReasons.None);
            p.Property("category.products.coreCategory.name", IncludeReasons.Default, ExcludeReasons.None);
            p.Property("category.products.coreCategory.products", IncludeReasons.None, ExcludeReasons.Default);
            p.Property("category.products.coreItems", IncludeReasons.Default, ExcludeReasons.None);
            p.Property("category.products.coreItems.description", IncludeReasons.Default, ExcludeReasons.None);
            p.Property("category.products.coreItems.id", IncludeReasons.Default, ExcludeReasons.None);
            p.Property("category.products.coreItems.name", IncludeReasons.Default, ExcludeReasons.None);
            p.Property("category.products.description", IncludeReasons.Default, ExcludeReasons.None);
            p.Property("category.products.hiddenCategory", IncludeReasons.None, ExcludeReasons.Default);
            p.Property("category.products.id", IncludeReasons.Default, ExcludeReasons.None);
            p.Property("category.products.items", IncludeReasons.None, ExcludeReasons.Default);
            p.Property("category.products.name", IncludeReasons.Default, ExcludeReasons.None);

            CoreCategorySelectedAndHidden(p);

            p.Property("ignoredCategory", IncludeReasons.Select, ExcludeReasons.Unselected);
            p.Property("ignoredCategory.description", IncludeReasons.Default, ExcludeReasons.None);
            p.Property("ignoredCategory.id", IncludeReasons.Default, ExcludeReasons.None);
            p.Property("ignoredCategory.name", IncludeReasons.Default, ExcludeReasons.None);
            p.Property("ignoredCategory.products", IncludeReasons.Default, ExcludeReasons.None);
            
            p.Property("ignoredCategory.products.category", IncludeReasons.None, ExcludeReasons.Default);
            p.Property("ignoredCategory.products.coreCategory", IncludeReasons.Default, ExcludeReasons.None);
            p.Property("ignoredCategory.products.coreCategory.description", IncludeReasons.Default, ExcludeReasons.None);
            p.Property("ignoredCategory.products.coreCategory.id", IncludeReasons.Default, ExcludeReasons.None);
            p.Property("ignoredCategory.products.coreCategory.name", IncludeReasons.Default, ExcludeReasons.None);
            p.Property("ignoredCategory.products.coreCategory.products", IncludeReasons.None, ExcludeReasons.Default);
            p.Property("ignoredCategory.products.coreItems", IncludeReasons.Default, ExcludeReasons.None);
            p.Property("ignoredCategory.products.coreItems.description", IncludeReasons.Default, ExcludeReasons.None);
            p.Property("ignoredCategory.products.coreItems.id", IncludeReasons.Default, ExcludeReasons.None);
            p.Property("ignoredCategory.products.coreItems.name", IncludeReasons.Default, ExcludeReasons.None);
            p.Property("ignoredCategory.products.description", IncludeReasons.Default, ExcludeReasons.None);
            p.Property("ignoredCategory.products.hiddenCategory", IncludeReasons.None, ExcludeReasons.Default);
            p.Property("ignoredCategory.products.id", IncludeReasons.Default, ExcludeReasons.None);
            p.Property("ignoredCategory.products.items", IncludeReasons.None, ExcludeReasons.Default);
            p.Property("ignoredCategory.products.name", IncludeReasons.Default, ExcludeReasons.None);

            HiddenCategorySelectedAndHidden(p);
        });
            
    [Fact]
    public void TraverseRqlExpression_WhenHidingAllPropertiesAndSelectingOne_propertiesAreHiddenAndSelectedPropertyIsSelected()
        => RunTest(string.Empty, string.Empty, "coreCategory,-*", p =>
        {
            p.Property("id", IncludeReasons.Default, ExcludeReasons.Unselected);
            p.Property("name", IncludeReasons.Default, ExcludeReasons.Unselected);
            p.Property("description", IncludeReasons.Default, ExcludeReasons.Unselected);
            
            p.Property("category", IncludeReasons.Default, ExcludeReasons.Unselected);
            p.Property("category.description", IncludeReasons.None, ExcludeReasons.Default);
            p.Property("category.id", IncludeReasons.None, ExcludeReasons.Default);
            p.Property("category.name", IncludeReasons.None, ExcludeReasons.Default);
            p.Property("category.products", IncludeReasons.None, ExcludeReasons.Default);
            
            CoreCategorySelectedAndHidden(p);
            
            p.Property("ignoredCategory", IncludeReasons.None, ExcludeReasons.Unselected);
            p.Property("ignoredCategory.description", IncludeReasons.None, ExcludeReasons.Default);
            p.Property("ignoredCategory.id", IncludeReasons.None, ExcludeReasons.Default);
            p.Property("ignoredCategory.name", IncludeReasons.None, ExcludeReasons.Default);
            p.Property("ignoredCategory.products", IncludeReasons.None, ExcludeReasons.Default);

            HiddenCategoryHidden(p);
            
            p.Property("items", IncludeReasons.Default, ExcludeReasons.Unselected);
            p.Property("items.description", IncludeReasons.None, ExcludeReasons.Default);
            p.Property("items.id", IncludeReasons.None, ExcludeReasons.Default);
            p.Property("items.name", IncludeReasons.None, ExcludeReasons.Default);
            
            p.Property("coreItems", IncludeReasons.Default, ExcludeReasons.Unselected);
            p.Property("coreItems.description", IncludeReasons.None, ExcludeReasons.Default);
            p.Property("coreItems.id", IncludeReasons.None, ExcludeReasons.Default);
            p.Property("coreItems.name", IncludeReasons.None, ExcludeReasons.Default);


        });
    
    [Fact]
    public void TraverseRqlExpression_WhenFilteringByHidden_ThenHiddenIsAdded()
        => RunTest("hiddenCategory.id=3", string.Empty, string.Empty, p =>
        {
            p.Property("hiddenCategory", IncludeReasons.Hierarchy, ExcludeReasons.Default);
            p.Property("hiddenCategory.id", IncludeReasons.Filter, ExcludeReasons.None);
        });

    [Fact]
    public void TraverseRqlExpression_WhenOrderingByHidden_ThenHiddenIsAdded()
        => RunTest(string.Empty, "hiddenCategory.id", string.Empty, p =>
        {
            p.Property("hiddenCategory", IncludeReasons.Hierarchy, ExcludeReasons.Default);
            p.Property("hiddenCategory.id", IncludeReasons.Order, ExcludeReasons.None);
        });

    private void RunTest(string filter, string order, string select, Action<GraphPrinter>? expectedCallback = null)
    {
        // Arrange
        var expected = new GraphPrinter();
        SetDefaultGraph(expected);
        expectedCallback?.Invoke(expected);

        // Act
        _filteringBuilder.TraverseRqlExpression(_queryContext.Graph, _rqlParser.Parse(filter));
        _orderingBuilder.TraverseRqlExpression(_queryContext.Graph, _rqlParser.Parse(order));
        _projectionBuilder.TraverseRqlExpression(_queryContext.Graph, _rqlParser.Parse(select));
        _projectionBuilder.BuildDefaults();
        var actual = new GraphPrinter();
        actual.Graph(_queryContext.Graph);

        // Assert 
        actual.Properties.Should().BeEquivalentTo(expected.Properties);
    }


    private static void SetDefaultGraph(GraphPrinter printer)
    {
        printer.Property("category", IncludeReasons.Default, ExcludeReasons.None);
        printer.Property("category.description", IncludeReasons.Default, ExcludeReasons.None);
        printer.Property("category.id", IncludeReasons.Default, ExcludeReasons.None);
        printer.Property("category.name", IncludeReasons.Default, ExcludeReasons.None);
        printer.Property("category.products", IncludeReasons.None, ExcludeReasons.Default);

        printer.Property("coreCategory", IncludeReasons.Default, ExcludeReasons.None);
        printer.Property("coreCategory.description", IncludeReasons.Default, ExcludeReasons.None);
        printer.Property("coreCategory.id", IncludeReasons.Default, ExcludeReasons.None);
        printer.Property("coreCategory.name", IncludeReasons.Default, ExcludeReasons.None);
        printer.Property("coreCategory.products", IncludeReasons.None, ExcludeReasons.Default);

        printer.Property("coreItems", IncludeReasons.Default, ExcludeReasons.None);
        printer.Property("coreItems.description", IncludeReasons.Default, ExcludeReasons.None);
        printer.Property("coreItems.id", IncludeReasons.Default, ExcludeReasons.None);
        printer.Property("coreItems.name", IncludeReasons.Default, ExcludeReasons.None);

        printer.Property("description", IncludeReasons.Default, ExcludeReasons.None);

        printer.Property("hiddenCategory", IncludeReasons.None, ExcludeReasons.Default);

        printer.Property("id", IncludeReasons.Default, ExcludeReasons.None);

        printer.Property("items", IncludeReasons.Default, ExcludeReasons.None);
        printer.Property("items.description", IncludeReasons.Default, ExcludeReasons.None);
        printer.Property("items.id", IncludeReasons.Default, ExcludeReasons.None);
        printer.Property("items.name", IncludeReasons.Default, ExcludeReasons.None);

        printer.Property("name", IncludeReasons.Default, ExcludeReasons.None);
    }
    
    private static void CoreCategoryHidden(GraphPrinter p)
    {
        p.Property("coreCategory", IncludeReasons.Default, ExcludeReasons.Unselected);
        p.Property("coreCategory.description", IncludeReasons.None, ExcludeReasons.Default);
        p.Property("coreCategory.id", IncludeReasons.None, ExcludeReasons.Default);
        p.Property("coreCategory.name", IncludeReasons.None, ExcludeReasons.Default);
        p.Property("coreCategory.products", IncludeReasons.None, ExcludeReasons.Default);
    }
    private static void CoreCategorySelectedAndHidden(GraphPrinter p)
    {
        p.Property("coreCategory", IncludeReasons.Default | IncludeReasons.Select, ExcludeReasons.Unselected);
        
        p.Property("coreCategory.products", IncludeReasons.Default, ExcludeReasons.None);
        p.Property("coreCategory.products.category", IncludeReasons.None, ExcludeReasons.Default);
        p.Property("coreCategory.products.coreCategory", IncludeReasons.Default, ExcludeReasons.None);
        p.Property("coreCategory.products.coreCategory.description", IncludeReasons.Default, ExcludeReasons.None);
        p.Property("coreCategory.products.coreCategory.id", IncludeReasons.Default, ExcludeReasons.None);
        p.Property("coreCategory.products.coreCategory.name", IncludeReasons.Default, ExcludeReasons.None);
        p.Property("coreCategory.products.coreCategory.products", IncludeReasons.None, ExcludeReasons.Default);
        p.Property("coreCategory.products.coreItems", IncludeReasons.Default, ExcludeReasons.None);
        p.Property("coreCategory.products.coreItems.description", IncludeReasons.Default, ExcludeReasons.None);
        p.Property("coreCategory.products.coreItems.id", IncludeReasons.Default, ExcludeReasons.None);
        p.Property("coreCategory.products.coreItems.name", IncludeReasons.Default, ExcludeReasons.None);
        p.Property("coreCategory.products.description", IncludeReasons.Default, ExcludeReasons.None);
        p.Property("coreCategory.products.hiddenCategory", IncludeReasons.None, ExcludeReasons.Default);
        p.Property("coreCategory.products.id", IncludeReasons.Default, ExcludeReasons.None);
        p.Property("coreCategory.products.items", IncludeReasons.None, ExcludeReasons.Default);
        p.Property("coreCategory.products.name", IncludeReasons.Default, ExcludeReasons.None);
    }
    
    private static void HiddenCategoryHidden(GraphPrinter p)
    {
        p.Property("hiddenCategory", IncludeReasons.None, ExcludeReasons.Default | ExcludeReasons.Unselected);
        p.Property("hiddenCategory.description", IncludeReasons.None, ExcludeReasons.Default);
        p.Property("hiddenCategory.id", IncludeReasons.None, ExcludeReasons.Default);
        p.Property("hiddenCategory.name", IncludeReasons.None, ExcludeReasons.Default);
        p.Property("hiddenCategory.products", IncludeReasons.None, ExcludeReasons.Default);
    }
    
    private static void HiddenCategorySelectedAndHidden(GraphPrinter p)
    {
        p.Property("hiddenCategory",  IncludeReasons.Select, ExcludeReasons.Default | ExcludeReasons.Unselected);
        p.Property("hiddenCategory.description", IncludeReasons.Default, ExcludeReasons.None);
        p.Property("hiddenCategory.id", IncludeReasons.Default, ExcludeReasons.None);
        p.Property("hiddenCategory.name", IncludeReasons.Default, ExcludeReasons.None);
        p.Property("hiddenCategory.products", IncludeReasons.Default, ExcludeReasons.None);
        
        p.Property("hiddenCategory.products.category", IncludeReasons.None, ExcludeReasons.Default);
        p.Property("hiddenCategory.products.coreCategory", IncludeReasons.Default, ExcludeReasons.None);
        p.Property("hiddenCategory.products.coreCategory.description", IncludeReasons.Default, ExcludeReasons.None);
        p.Property("hiddenCategory.products.coreCategory.id", IncludeReasons.Default, ExcludeReasons.None);
        p.Property("hiddenCategory.products.coreCategory.name", IncludeReasons.Default, ExcludeReasons.None);
        p.Property("hiddenCategory.products.coreCategory.products", IncludeReasons.None, ExcludeReasons.Default);
        p.Property("hiddenCategory.products.coreItems", IncludeReasons.Default, ExcludeReasons.None);
        p.Property("hiddenCategory.products.coreItems.description", IncludeReasons.Default, ExcludeReasons.None);
        p.Property("hiddenCategory.products.coreItems.id", IncludeReasons.Default, ExcludeReasons.None);
        p.Property("hiddenCategory.products.coreItems.name", IncludeReasons.Default, ExcludeReasons.None);
        p.Property("hiddenCategory.products.description", IncludeReasons.Default, ExcludeReasons.None);
        p.Property("hiddenCategory.products.hiddenCategory", IncludeReasons.None, ExcludeReasons.Default);
        p.Property("hiddenCategory.products.id", IncludeReasons.Default, ExcludeReasons.None);
        p.Property("hiddenCategory.products.items", IncludeReasons.None, ExcludeReasons.Default);
        p.Property("hiddenCategory.products.name", IncludeReasons.Default, ExcludeReasons.None);
    }
}
