using FluentAssertions;
using Moq;
using Mpt.Rql;
using Mpt.Rql.Abstractions;
using Mpt.Rql.Abstractions.Configuration;
using Mpt.Rql.Abstractions.Group;
using Mpt.Rql.Core;
using Mpt.Rql.Core.Metadata;
using Mpt.Rql.Parsers.Linear.Services;
using Mpt.Rql.Services.Context;
using Mpt.Rql.Services.Filtering.Builders;
using Mpt.Rql.Services.Ordering;
using Mpt.Rql.Services.Ordering.Functions;
using Mpt.Rql.Settings;
using Rql.Tests.Unit.Services.Models;
using System.Linq.Expressions;
using Xunit;

namespace Rql.Tests.Unit.Ordering;

public class FirstOrderingFunctionTests
{
    /// <summary>
    /// Builds a context for the given order string. The graph contains only a root with an
    /// <c>items</c> child (so descent into the collection scope works); the filtering builder is a
    /// mock that always yields <c>e.Name == "x"</c> against whatever element parameter it receives.
    /// </summary>
    private static (FirstOrderingFunction Function, OrderingFunctionContext Context, BuilderContext BuilderContext) Make(
        string order,
        NavigationStrategy navigation = NavigationStrategy.Default)
    {
        var validator = new Mock<IActionValidator>();
        validator.Setup(v => v.Validate(It.IsAny<RqlPropertyInfo>(), It.IsAny<RqlActions>())).Returns(true);

        IMetadataProvider metadata = new MetadataProvider(new PropertyNameProvider(), new MetadataFactory(new GlobalRqlSettings()));

        var root = RqlNode.MakeRoot();
        metadata.TryGetPropertyByDisplayName(typeof(Product), "items", out var items);
        root.IncludeChild(items!, IncludeReasons.Hierarchy);
        var builderContext = new BuilderContext();
        builderContext.SetNode(root);

        var settings = new RqlSettings { Ordering = { Navigation = navigation } };
        var pathBuilder = new OrderingPathInfoBuilder(validator.Object, metadata, builderContext, settings, Mock.Of<IExternalServiceAccessor>());

        var filterBuilder = new Mock<IExpressionBuilder>();
        filterBuilder
            .Setup(b => b.Build(It.IsAny<ParameterExpression>(), It.IsAny<RqlExpression>()))
            .Returns((ParameterExpression e, RqlExpression _) =>
                new Result<Expression>(Expression.Equal(Expression.Property(e, nameof(Item.Name)), Expression.Constant("x"))));

        var group = (RqlGenericGroup)new RqlParser().Parse(order);
        var context = new OrderingFunctionContext(
            Expression.Parameter(typeof(Product), "p"),
            group.Items!,
            pathBuilder,
            filterBuilder.Object,
            builderContext,
            settings);

        return (new FirstOrderingFunction(), context, builderContext);
    }

    private static Func<Product, TKey> Compile<TKey>(Expression key, OrderingFunctionContext context)
        => Expression.Lambda<Func<Product, TKey>>(key, context.Root).Compile();

    [Fact]
    public void Name_IsFirst()
        => new FirstOrderingFunction().Name.Should().Be("first");

    [Fact]
    public void Build_ThreeArguments_ReturnsSelectorOfFirstMatchingElement()
    {
        var (function, context, _) = Make("+first(items,eq(name,x),id)");

        var result = function.Build(context);

        result.IsError.Should().BeFalse();
        var key = Compile<int?>(result.Value!, context);
        key(new Product { Items = [new Item { Id = 5, Name = "x" }, new Item { Id = 7, Name = "x" }] }).Should().Be(5);
        key(new Product { Items = [new Item { Id = 5, Name = "y" }] }).Should().BeNull();   // no match -> null
        key(new Product { Items = [] }).Should().BeNull();                                     // empty -> null
    }

    [Fact]
    public void Build_TwoArguments_UsesFirstElementWithoutPredicate()
    {
        var (function, context, _) = Make("+first(items,description)");

        var result = function.Build(context);

        result.IsError.Should().BeFalse();
        result.Value!.Type.Should().Be(typeof(string));
        var key = Compile<string?>(result.Value!, context);
        key(new Product { Items = [new Item { Description = "b" }, new Item { Description = "a" }] }).Should().Be("b");
        key(new Product { Items = [] }).Should().BeNull();
    }

    [Fact]
    public void Build_ValueTypeSelector_IsLiftedToNullable()
    {
        var (function, context, _) = Make("+first(items,eq(name,x),id)");

        var result = function.Build(context);

        result.Value!.Type.Should().Be(typeof(int?));
    }

    [Fact]
    public void Build_DefaultNavigation_DoesNotWrapInNullCheck()
    {
        var (function, context, _) = Make("+first(items,eq(name,x),id)");

        var result = function.Build(context);

        // Expression factories return internal subclasses (e.g. MethodCallExpression1), so assert assignability.
        result.Value.Should().BeAssignableTo<MethodCallExpression>();
    }

    [Fact]
    public void Build_SafeNavigation_ReturnsNullForNullCollection()
    {
        var (function, context, _) = Make("+first(items,eq(name,x),id)", NavigationStrategy.Safe);

        var result = function.Build(context);

        result.IsError.Should().BeFalse();
        result.Value.Should().BeAssignableTo<ConditionalExpression>();
        var key = Compile<int?>(result.Value!, context);
        key(new Product { Items = null! }).Should().BeNull();
        key(new Product { Items = [new Item { Id = 3, Name = "x" }] }).Should().Be(3);
    }

    [Theory]
    [InlineData("+first(items)", 1)]
    [InlineData("+first(items,eq(name,x),id,extra)", 4)]
    public void Build_WrongArity_ReturnsFunctionArgumentsError(string order, int count)
    {
        var (function, context, _) = Make(order);

        var result = function.Build(context);

        result.IsError.Should().BeTrue();
        var error = result.Errors.Single();
        error.Code.Should().Be(OrderingErrorCodes.FunctionArguments);
        error.Message.Should().Be($"'first' requires 2 or 3 arguments: (collection, [predicate,] path). Got {count}.");
    }

    [Fact]
    public void Build_CollectionArgumentNotAPath_ReturnsFunctionArgumentsError()
    {
        var (function, context, _) = Make("+first(eq(name,x),id)");

        var result = function.Build(context);

        result.Errors.Single().Code.Should().Be(OrderingErrorCodes.FunctionArguments);
        result.Errors.Single().Message.Should().Be("'first': collection argument must be a property path.");
    }

    [Fact]
    public void Build_PathArgumentNotAPath_ReturnsFunctionArgumentsError()
    {
        var (function, context, _) = Make("+first(items,eq(name,x),eq(id,1))");

        var result = function.Build(context);

        result.Errors.Single().Code.Should().Be(OrderingErrorCodes.FunctionArguments);
        result.Errors.Single().Message.Should().Be("'first': path argument must be a property path.");
    }

    [Fact]
    public void Build_CollectionIsNotACollection_ReturnsNotCollectionError()
    {
        var (function, context, _) = Make("+first(name,eq(name,x),id)");

        var result = function.Build(context);

        var error = result.Errors.Single();
        error.Code.Should().Be(OrderingErrorCodes.NotCollection);
        error.Message.Should().Be("'name' is not a collection property.");
        error.Path.Should().Be("name");
    }

    [Fact]
    public void Build_UnknownCollection_PropagatesPathBuilderError()
    {
        var (function, context, _) = Make("+first(nope,eq(name,x),id)");

        var result = function.Build(context);

        result.Errors.Single().Message.Should().Be("Invalid property path.");
        result.Errors.Single().Path.Should().Be("nope");
    }

    [Fact]
    public void Build_UnknownSelector_PropagatesPathBuilderErrorWithCollectionPrefix()
    {
        var (function, context, _) = Make("+first(items,eq(name,x),nope)");

        var result = function.Build(context);

        result.Errors.Single().Message.Should().Be("Invalid property path.");
        result.Errors.Single().Path.Should().Be("items.nope");
    }

    [Theory]
    [InlineData("+first(category.products,eq(name,x),coreCategory)")] // reference
    [InlineData("+first(category.products,eq(name,x),items)")]        // collection
    public void Build_SelectorNotPrimitive_ReturnsNotPrimitiveError(string order)
    {
        var (function, context, _) = Make(order);

        var result = function.Build(context);

        var error = result.Errors.Single();
        error.Code.Should().Be(OrderingErrorCodes.NotPrimitive);
        error.Message.Should().Be("'first': path must resolve to a primitive property.");
    }

    [Fact]
    public void Build_DottedCollectionPath_Works()
    {
        var (function, context, _) = Make("+first(category.products,eq(name,x),description)");

        var result = function.Build(context);

        result.IsError.Should().BeFalse();
        var key = Compile<string?>(result.Value!, context);
        var product = new Product
        {
            Category = new Category { Products = [new Product { Name = "x", Description = "inner" }] }
        };
        key(product).Should().Be("inner");
    }

    [Theory]
    [InlineData("+first(items,eq(name,x),id)")]
    [InlineData("+first(items,eq(name,x),nope)")]
    [InlineData("+first(name,eq(name,x),id)")]
    public void Build_AlwaysReturnsBuilderContextToRoot(string order)
    {
        var (function, context, builderContext) = Make(order);

        function.Build(context);

        builderContext.CurrentNode.Should().NotBeNull();
        builderContext.CurrentNode!.Parent.Should().BeNull();
    }
}
