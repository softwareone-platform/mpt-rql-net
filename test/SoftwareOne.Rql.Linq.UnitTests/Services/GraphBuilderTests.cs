using FluentAssertions;
using Moq;
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

namespace SoftwareOne.Rql.Linq.UnitTests.Services
{
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

            var generalSettings = new RqlGeneralSettings { };
            var selectSettings = new RqlSelectSettings { Explicit = RqlSelectModes.All, Implicit = RqlSelectModes.Core | RqlSelectModes.Primitive };
            var metadataProvider = new MetadataProvider(new PropertyNameProvider(), new MetadataFactory(generalSettings));

            _projectionBuilder = new ProjectionGraphBuilder<Product>(_queryContext, metadataProvider, actionValidatorMock.Object, selectSettings);
            _filteringBuilder = new FilteringGraphBuilder<Product>(metadataProvider, actionValidatorMock.Object);
            _orderingBuilder = new OrderingGraphBuilder<Product>(metadataProvider, actionValidatorMock.Object);
        }

        [Fact]
        public void TraverseRqlExpression_WithEmptyExpression_BuildsDefaultGraph()
            => RunTest(string.Empty, string.Empty, string.Empty);

        [Fact]
        public void TraverseRqlExpression_WhenHidingProperty_propertyIsHidden()
            => RunTest(string.Empty, string.Empty, "-coreCategory", p =>
            {
                p.Property("coreCategory", IncludeReasons.Default, ExcludeReasons.Unselected);
                p.Remove("coreCategory.description");
                p.Remove("coreCategory.id");
                p.Remove("coreCategory.name");
                p.Remove("coreCategory.products");
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
    }
}
