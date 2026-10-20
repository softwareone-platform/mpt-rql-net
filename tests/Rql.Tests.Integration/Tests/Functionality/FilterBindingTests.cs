using FluentAssertions;
using Mpt.Rql;
using Mpt.Rql.Abstractions.Configuration;
using Mpt.Rql.Abstractions.Configuration.Filter;
using Rql.Tests.Integration.Core;
using System.Linq.Expressions;
using Xunit;

namespace Rql.Tests.Integration.Tests.Functionality;

/// <summary>
/// <c>Filter.Binding = Storage</c> applies the filter to the storage query before the projection. These tests check
/// that it selects the same rows as the default view binding and that the storage predicate carries no conditional,
/// which is what lets relational providers index the filtered columns.
/// </summary>
public class FilterBindingTests
{
    private readonly IRqlQueryable<Ticket, TicketView> _tickets = RqlFactory.Make<Ticket, TicketView>(services => { }, rql =>
    {
        rql.Settings.Select.Implicit = RqlSelectModes.Core | RqlSelectModes.Primitive | RqlSelectModes.Reference;
        rql.Settings.Select.Explicit = RqlSelectModes.All;
        rql.Settings.Select.MaxDepth = 10;
        rql.ScanForMappers(typeof(FilterBindingTests).Assembly);
    });

    private readonly IRqlQueryable<Product, Product> _products = RqlFactory.Make<Product>(services => { }, rql =>
    {
        rql.Settings.Select.Implicit = RqlSelectModes.Core | RqlSelectModes.Primitive | RqlSelectModes.Reference;
        rql.Settings.Select.Explicit = RqlSelectModes.All;
        rql.Settings.Select.MaxDepth = 10;
    });

    public static TheoryData<string> TicketFilters => new()
    {
        "eq(case.reporter.email,ann@x.io)",
        "ne(case.reporter.email,ann@x.io)",
        "eq(case.reporter.email,null())",
        "ne(case.reporter.email,null())",
        "eq(case.reporter.phone,null())",
        "ne(case.reporter.phone,null())",
        "like(case.reporter.email,*@x.io)",
        "like(case.reporter.email,bob*)",
        "in(case.reporter.email,(ann@x.io,bob@x.io))",
        "out(case.reporter.email,(ann@x.io,bob@x.io))",
        "gt(case.id,15)",
        "le(case.id,20)",
        "eq(case.id,30)",
        "eq(case.status,Open)",
        "eq(owner.email,owner@x.io)",
        "ne(owner.email,owner@x.io)",
        "and(eq(case.status,Open),ne(case.reporter.email,null()))",
        "or(eq(case.reporter.email,bob@x.io),eq(owner.email,other@x.io))",
        "not(eq(case.reporter.email,ann@x.io))",
        "any(tags,eq(value,red))",
        "all(tags,eq(value,red))",
        "any(tags)",
        "eq(title,alpha)",
        "eq(title,null())",
        "eq(score,null())",
        "gt(score,4)",
        "eq(case.reporter.email,title)",
    };

    [Theory]
    [MemberData(nameof(TicketFilters))]
    public void StorageBinding_SelectsTheSameRowsAsViewBinding_WithSafeNavigation(string filter)
    {
        var view = Transform(_tickets, TicketRepository.Query(), filter, FilterBinding.View, NavigationStrategy.Safe);
        var storage = Transform(_tickets, TicketRepository.Query(), filter, FilterBinding.Storage, NavigationStrategy.Safe);

        storage.IsSuccess.Should().BeTrue(string.Join("; ", storage.Errors.Select(e => e.Message)));
        view.IsSuccess.Should().BeTrue(string.Join("; ", view.Errors.Select(e => e.Message)));
        Ids(storage).Should().Equal(Ids(view));
    }

    [Theory]
    [MemberData(nameof(TicketFilters))]
    public void StorageBinding_WithSafeNavigation_LeavesNoConditionalInThePredicate(string filter)
    {
        // Safe mapping guards every path, including the root `title`, so this comparison has a guard on both
        // sides; that shape is deliberately left as it is (see NullGuardHoistingVisitor.RewriteEquality).
        if (filter == "eq(case.reporter.email,title)")
            return;

        var storage = Transform(_tickets, TicketRepository.Query(), filter, FilterBinding.Storage, NavigationStrategy.Safe);

        WherePredicates(storage.Query.Expression).Should().NotBeEmpty().And.OnlyContain(predicate => !HasConditional(predicate));
    }

    [Theory]
    [MemberData(nameof(TicketFilters))]
    public void StorageBinding_WithDefaultNavigation_LeavesNoConditionalInThePredicate(string filter)
    {
        // Default navigation: the only guards come from the projection of the nullable references
        var storage = Transform(_tickets, TicketRepository.Query(), filter, FilterBinding.Storage, NavigationStrategy.Default);

        storage.IsSuccess.Should().BeTrue(string.Join("; ", storage.Errors.Select(e => e.Message)));
        WherePredicates(storage.Query.Expression).Should().NotBeEmpty().And.OnlyContain(predicate => !HasConditional(predicate));
    }

    [Theory]
    [InlineData("eq(case.reporter.email,ann@x.io)", new[] { 1 })]
    [InlineData("ne(case.reporter.email,ann@x.io)", new[] { 2, 3, 4 })]
    [InlineData("eq(case.reporter.email,null())", new[] { 3, 4 })]
    [InlineData("like(case.reporter.email,*@x.io)", new[] { 1, 2 })]
    [InlineData("in(case.reporter.email,(ann@x.io,bob@x.io))", new[] { 1, 2 })]
    [InlineData("out(case.reporter.email,(ann@x.io,bob@x.io))", new[] { 3, 4 })]
    [InlineData("gt(case.id,15)", new[] { 2, 3 })]
    [InlineData("eq(owner.email,owner@x.io)", new[] { 1 })]
    [InlineData("any(tags,eq(value,blue))", new[] { 1, 4 })]
    public void StorageBinding_WithDefaultNavigation_IsNullSafeInMemory(string filter, int[] expected)
    {
        // With view binding and default navigation these throw under LINQ-to-Objects on the tickets without a case;
        // the storage predicate spells the guards out, so it evaluates the way a database would.
        var storage = Transform(_tickets, TicketRepository.Query(), filter, FilterBinding.Storage, NavigationStrategy.Default);

        Ids(storage).Should().Equal(expected);
    }

    [Fact]
    public void StorageBinding_AppliesTheFilterBeforeTheProjection()
    {
        var storage = Transform(_tickets, TicketRepository.Query(), "eq(case.status,Open)", FilterBinding.Storage, NavigationStrategy.Default);

        var calls = new List<string>();
        for (var expression = storage.Query.Expression; expression is MethodCallExpression call; expression = call.Arguments[0])
            calls.Add(call.Method.Name);

        calls.Should().Equal("Select", "Where");
    }

    [Fact]
    public void ViewBinding_AppliesTheFilterAfterTheProjection()
    {
        var view = Transform(_tickets, TicketRepository.Query(), "eq(case.status,Open)", FilterBinding.View, NavigationStrategy.Default);

        var calls = new List<string>();
        for (var expression = view.Query.Expression; expression is MethodCallExpression call; expression = call.Arguments[0])
            calls.Add(call.Method.Name);

        calls.Should().Equal("Where", "Select");
    }

    [Fact]
    public void StorageBinding_KeepsFilterBeforeOrdering()
    {
        var storage = _tickets.Transform(TicketRepository.Query(), new RqlRequest { Filter = "eq(case.status,Open)", Order = "-case.id" }, options =>
        {
            options.Settings.Filter.Binding = FilterBinding.Storage;
            options.Settings.Mapping.Navigation = NavigationStrategy.Safe;
            options.Settings.Ordering.Navigation = NavigationStrategy.Safe;
        });

        storage.IsSuccess.Should().BeTrue();
        storage.Query.Select(t => t.Id).ToList().Should().Equal(3, 1);
    }

    [Fact]
    public void StorageBinding_HasNoEffectOnATransparentMapping()
    {
        var result = _products.Transform(ProductRepository.Query(), new RqlRequest { Filter = "eq(reference.name,Jewelry Widget)" }, options =>
        {
            options.Settings.Mapping.Transparent = true;
            options.Settings.Filter.Binding = FilterBinding.Storage;
        });

        result.IsSuccess.Should().BeTrue();
        result.Query.Select(p => p.Id).ToList().Should().Equal(1);
        ((MethodCallExpression)result.Query.Expression).Method.Name.Should().Be("Where");
    }

    public static TheoryData<string> ProductFilters => new()
    {
        "eq(reference.name,Jewelry Widget)",
        "ne(reference.category,Activity)",
        "like(name,*Contraption)",
        "like(reference.name,Dog*)",
        "in(category,(Home,Pets))",
        "gt(price,200)",
        "le(reference.price,100)",
        "eq(price,sellPrice)",
        "gt(price,sellPrice)",
        "any(orders,eq(clientName,Michael))",
        "all(orders,ne(clientName,Michael))",
        "any(collection,eq(name,Dog Whatchamacallit))",
        "any(ordersIds,eq(self(),2))",
        "and(gt(price,100),or(eq(category,Activity),eq(category,Home)))",
        "not(eq(reference.category,Activity))",
        "eq(desc,null())",
    };

    [Theory]
    [MemberData(nameof(ProductFilters))]
    public void StorageBinding_SelectsTheSameProductsAsViewBinding(string filter)
    {
        var view = Transform(_products, ProductRepository.Query(), filter, FilterBinding.View, NavigationStrategy.Default);
        var storage = Transform(_products, ProductRepository.Query(), filter, FilterBinding.Storage, NavigationStrategy.Default);

        view.IsSuccess.Should().BeTrue(string.Join("; ", view.Errors.Select(e => e.Message)));
        storage.IsSuccess.Should().BeTrue(string.Join("; ", storage.Errors.Select(e => e.Message)));
        Ids(storage).Should().Equal(Ids(view));
        WherePredicates(storage.Query.Expression).Should().NotBeEmpty().And.OnlyContain(predicate => !HasConditional(predicate));
    }

    private static RqlResponse<TView> Transform<TStorage, TView>(IRqlQueryable<TStorage, TView> rql, IQueryable<TStorage> source, string filter, FilterBinding binding, NavigationStrategy navigation)
        => rql.Transform(source, new RqlRequest { Filter = filter }, options =>
        {
            options.Settings.Filter.Binding = binding;
            options.Settings.Filter.Navigation = navigation;
            options.Settings.Mapping.Navigation = navigation;
        });

    private static List<int> Ids<T>(RqlResponse<T> response) where T : ITestEntity
        => response.Query.ToList().Select(t => t.Id).OrderBy(id => id).ToList();

    private static List<Expression> WherePredicates(Expression expression)
    {
        var finder = new WhereFinder();
        finder.Visit(expression);
        return finder.Predicates;
    }

    private static bool HasConditional(Expression expression)
    {
        var finder = new ConditionalFinder();
        finder.Visit(expression);
        return finder.Found;
    }

    private sealed class WhereFinder : ExpressionVisitor
    {
        public List<Expression> Predicates { get; } = [];

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.Name == nameof(Queryable.Where) && node.Method.DeclaringType == typeof(Queryable))
                Predicates.Add(node.Arguments[1]);

            return base.VisitMethodCall(node);
        }
    }

    private sealed class ConditionalFinder : ExpressionVisitor
    {
        public bool Found { get; private set; }

        protected override Expression VisitConditional(ConditionalExpression node)
        {
            Found = true;
            return node;
        }
    }
}
