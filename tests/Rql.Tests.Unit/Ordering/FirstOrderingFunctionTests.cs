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
using System.Collections.Immutable;
using System.Linq.Expressions;
using Xunit;

namespace Rql.Tests.Unit.Ordering;

public class FirstOrderingFunctionTests
{
    /// <summary>Root type whose only collection is a struct enumerable — not reference-assignable to IEnumerable&lt;T&gt;.</summary>
    private sealed class FrozenHolder
    {
        public ImmutableArray<Item> Items { get; set; }
    }

    private sealed record Harness(FirstOrderingFunction Function, OrderingFunctionContext Context, BuilderContext BuilderContext, RqlNode Root);

    /// <summary>
    /// Builds a context for the given order string against <typeparamref name="TRoot"/>. For <see cref="Product"/> the
    /// graph is seeded with <c>items</c> and <c>category → products</c> (so descent into collection scopes works); the
    /// filtering builder is a mock that always yields <c>e.Name == "x"</c> against whatever element parameter it receives.
    /// </summary>
    private static Harness Make<TRoot>(string order, NavigationStrategy navigation = NavigationStrategy.Default)
    {
        var validator = new Mock<IActionValidator>();
        validator.Setup(v => v.Validate(It.IsAny<RqlPropertyInfo>(), It.IsAny<RqlActions>())).Returns(true);

        IMetadataProvider metadata = new MetadataProvider(new PropertyNameProvider(), new MetadataFactory(new GlobalRqlSettings()));

        var root = RqlNode.MakeRoot();
        if (typeof(TRoot) == typeof(Product))
        {
            metadata.TryGetPropertyByDisplayName(typeof(Product), "items", out var items);
            metadata.TryGetPropertyByDisplayName(typeof(Product), "category", out var category);
            metadata.TryGetPropertyByDisplayName(typeof(Category), "products", out var products);
            root.IncludeChild(items!, IncludeReasons.Hierarchy);
            root.IncludeChild(category!, IncludeReasons.Hierarchy).IncludeChild(products!, IncludeReasons.Hierarchy);
        }

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
            Expression.Parameter(typeof(TRoot), "p"),
            group.Items!,
            pathBuilder,
            filterBuilder.Object,
            builderContext,
            settings);

        return new Harness(new FirstOrderingFunction(), context, builderContext, root);
    }

    private static Harness Make(string order, NavigationStrategy navigation = NavigationStrategy.Default)
        => Make<Product>(order, navigation);

    private static Func<Product, TKey> Compile<TKey>(Expression key, OrderingFunctionContext context)
        => Expression.Lambda<Func<Product, TKey>>(key, context.Root).Compile();

    [Fact]
    public void Name_IsFirst()
        => new FirstOrderingFunction().Name.Should().Be("first");

    [Fact]
    public void Build_ThreeArguments_ReturnsSelectorOfFirstMatchingElement()
    {
        var h = Make("+first(items,eq(name,x),id)");

        var result = h.Function.Build(h.Context);

        result.IsError.Should().BeFalse();
        var key = Compile<int?>(result.Value!, h.Context);
        key(new Product { Items = [new Item { Id = 5, Name = "x" }, new Item { Id = 7, Name = "x" }] }).Should().Be(5);
        key(new Product { Items = [new Item { Id = 5, Name = "y" }] }).Should().BeNull();
        key(new Product { Items = [] }).Should().BeNull();
    }

    [Fact]
    public void Build_TwoArguments_UsesFirstElementWithoutPredicate()
    {
        var h = Make("+first(items,description)");

        var result = h.Function.Build(h.Context);

        result.IsError.Should().BeFalse();
        result.Value!.Type.Should().Be(typeof(string));
        var key = Compile<string?>(result.Value!, h.Context);
        key(new Product { Items = [new Item { Description = "b" }, new Item { Description = "a" }] }).Should().Be("b");
        key(new Product { Items = [] }).Should().BeNull();
    }

    [Fact]
    public void Build_ValueTypeSelector_IsLiftedToNullable()
    {
        var h = Make("+first(items,eq(name,x),id)");

        h.Function.Build(h.Context).Value!.Type.Should().Be(typeof(int?));
    }

    [Theory]
    [InlineData(NavigationStrategy.Default)]
    [InlineData(NavigationStrategy.Safe)]
    public void Build_NeverWrapsTheKeyInACollectionNullCheck(NavigationStrategy navigation)
    {
        // EF Core cannot translate `collection == null ? ... : ...` for collection navigations, so the key is
        // always the bare Where/Select/FirstOrDefault chain (a null collection throws in-memory, like any()).
        var h = Make("+first(items,eq(name,x),id)", navigation);

        var result = h.Function.Build(h.Context);

        result.IsError.Should().BeFalse();
        result.Value.Should().BeAssignableTo<MethodCallExpression>();
    }

    [Fact]
    public void Build_BuildsThePredicateUnderTheOrderingNavigation_AndRestoresTheFilterSetting()
    {
        var h = Make("+first(items,eq(name,x),id)", NavigationStrategy.Safe);
        h.Context.Settings.Filter.Navigation.Should().Be(NavigationStrategy.Default);
        NavigationStrategy? observed = null;
        var filterBuilder = new Mock<IExpressionBuilder>();
        filterBuilder
            .Setup(b => b.Build(It.IsAny<ParameterExpression>(), It.IsAny<RqlExpression>()))
            .Returns((ParameterExpression e, RqlExpression _) =>
            {
                observed = h.Context.Settings.Filter.Navigation;
                return new Result<Expression>(Expression.Equal(Expression.Property(e, nameof(Item.Name)), Expression.Constant("x")));
            });
        var context = h.Context with { FilterBuilder = filterBuilder.Object };

        h.Function.Build(context);

        observed.Should().Be(NavigationStrategy.Safe);
        h.Context.Settings.Filter.Navigation.Should().Be(NavigationStrategy.Default);
    }

    [Fact]
    public void Build_SafeNavigation_DottedCollection_GuardsThePrefixAroundTheKey()
    {
        var h = Make("+first(category.products,eq(name,x),description)", NavigationStrategy.Safe);

        var result = h.Function.Build(h.Context);

        result.IsError.Should().BeFalse();
        // `category == null ? null : category.products.Where(..).Select(..).FirstOrDefault()` — the null test is on the
        // reference prefix (translatable), never on the collection itself.
        var guard = result.Value.Should().BeAssignableTo<ConditionalExpression>().Subject;
        guard.IfFalse.Should().BeAssignableTo<MethodCallExpression>();
        var key = Compile<string?>(result.Value!, h.Context);
        key(new Product { Category = null! }).Should().BeNull();
        key(new Product { Category = new Category { Products = [new Product { Name = "x", Description = "inner" }] } }).Should().Be("inner");
    }

    [Theory]
    [InlineData("+first(items)", 1)]
    [InlineData("+first(items,eq(name,x),id,extra)", 4)]
    public void Build_WrongArity_ReturnsFunctionArgumentsError(string order, int count)
    {
        var h = Make(order);

        var result = h.Function.Build(h.Context);

        var error = result.Errors.Single();
        error.Code.Should().Be(OrderingErrorCodes.FunctionArguments);
        error.Message.Should().Be($"'first' requires 2 or 3 arguments: (collection, [predicate,] path). Got {count}.");
    }

    [Fact]
    public void Build_CollectionArgumentNotAPath_ReturnsFunctionArgumentsError()
    {
        var h = Make("+first(eq(name,x),id)");

        var result = h.Function.Build(h.Context);

        result.Errors.Single().Code.Should().Be(OrderingErrorCodes.FunctionArguments);
        result.Errors.Single().Message.Should().Be("'first': collection argument must be a property path.");
    }

    [Fact]
    public void Build_PathArgumentNotAPath_ReturnsFunctionArgumentsError()
    {
        var h = Make("+first(items,eq(name,x),eq(id,1))");

        var result = h.Function.Build(h.Context);

        result.Errors.Single().Code.Should().Be(OrderingErrorCodes.FunctionArguments);
        result.Errors.Single().Message.Should().Be("'first': path argument must be a property path.");
    }

    [Fact]
    public void Build_CollectionIsNotACollection_ReturnsNotCollectionError()
    {
        var h = Make("+first(name,eq(name,x),id)");

        var error = h.Function.Build(h.Context).Errors.Single();

        error.Code.Should().Be(OrderingErrorCodes.NotCollection);
        error.Message.Should().Be("'name' is not a collection property.");
        error.Path.Should().Be("name");
    }

    [Fact]
    public void Build_StructEnumerableCollection_ReturnsNotCollectionErrorInsteadOfThrowing()
    {
        var h = Make<FrozenHolder>("+first(items,eq(name,x),id)");

        var error = h.Function.Build(h.Context).Errors.Single();

        error.Code.Should().Be(OrderingErrorCodes.NotCollection);
        error.Path.Should().Be("items");
    }

    [Fact]
    public void Build_UnknownCollection_PropagatesPathBuilderError()
    {
        var h = Make("+first(nope,eq(name,x),id)");

        var error = h.Function.Build(h.Context).Errors.Single();

        error.Message.Should().Be("Invalid property path.");
        error.Path.Should().Be("nope");
    }

    [Fact]
    public void Build_UnknownSelector_PropagatesPathBuilderErrorWithCollectionPrefix()
    {
        var h = Make("+first(items,eq(name,x),nope)");

        var error = h.Function.Build(h.Context).Errors.Single();

        error.Message.Should().Be("Invalid property path.");
        error.Path.Should().Be("items.nope");
    }

    [Theory]
    [InlineData("+first(category.products,eq(name,x),nope)")]
    [InlineData("+first(Category.PRODUCTS,eq(name,x),nope)")] // graph lookups are case-insensitive, like metadata
    public void Build_UnknownSelector_DottedCollection_PrefixesTheFullPath(string order)
    {
        var h = Make(order);

        var error = h.Function.Build(h.Context).Errors.Single();

        error.Message.Should().Be("Invalid property path.");
        error.Path.Should().Be("category.products.nope");
    }

    [Theory]
    [InlineData("+first(category.products,eq(name,x),coreCategory)")] // reference
    [InlineData("+first(category.products,eq(name,x),items)")]        // collection
    public void Build_SelectorNotPrimitive_ReturnsNotPrimitiveError(string order)
    {
        var h = Make(order);

        var error = h.Function.Build(h.Context).Errors.Single();

        error.Code.Should().Be(OrderingErrorCodes.NotPrimitive);
        error.Message.Should().Be("'first': path must resolve to a primitive property.");
        error.Path.Should().StartWith("category.products.");
    }

    [Fact]
    public void Build_DottedCollectionPath_Works()
    {
        var h = Make("+first(category.products,eq(name,x),description)");

        var result = h.Function.Build(h.Context);

        result.IsError.Should().BeFalse();
        var key = Compile<string?>(result.Value!, h.Context);
        var product = new Product
        {
            Category = new Category { Products = [new Product { Name = "x", Description = "inner" }] }
        };
        key(product).Should().Be("inner");
    }

    [Theory]
    [InlineData("+first(items,eq(name,x),id)")]
    [InlineData("+first(items,eq(name,x),nope)")]
    [InlineData("+first(category.products,eq(name,x),nope)")]
    [InlineData("+first(name,eq(name,x),id)")]
    public void Build_AlwaysReturnsBuilderContextToTheRootNode(string order)
    {
        var h = Make(order);

        h.Function.Build(h.Context);

        h.BuilderContext.CurrentNode.Should().BeSameAs(h.Root);
        h.Context.Settings.Filter.Navigation.Should().Be(NavigationStrategy.Default);
    }
}
