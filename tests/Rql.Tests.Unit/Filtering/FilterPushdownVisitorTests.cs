using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Mpt.Rql;
using Mpt.Rql.Abstractions.Configuration;
using Mpt.Rql.Abstractions.Configuration.Filter;
using Mpt.Rql.Services.Filtering;
using System.Linq.Expressions;
using Xunit;

namespace Rql.Tests.Unit.Filtering;

public class FilterPushdownVisitorTests
{
    // the shape MappingService builds: nullable references guarded, collections projected to a list
    private static readonly Expression<Func<Order, OrderView>> Projection = o => new OrderView
    {
        Number = o.Number,
        Customer = o.Customer != null
            ? new CustomerView
            {
                Id = o.Customer.Id,
                Code = o.Customer.Name,
                Label = o.Customer.Name ?? "none",
                Address = o.Customer.Address != null ? new AddressView { City = o.Customer.Address.City } : null
            }
            : null,
        Lines = o.Lines.Select(l => new LineView { Sku = l.Sku }).ToList()
    };

    private static readonly IRqlQueryable<Order, OrderView> Rql =
        new ServiceCollection().AddRql().BuildServiceProvider().GetRequiredService<IRqlQueryable<Order, OrderView>>();

    [Fact]
    public void ToSourceFilter_RootMember_BindsTheStorageMember()
    {
        ToSourceFilter(v => v.Number == "Q1").Body.ToString().Should().Be("(o.Number == \"Q1\")");
    }

    [Fact]
    public void ToSourceFilter_NestedNullableReferences_DropsTheGuards()
    {
        ToSourceFilter(v => v.Customer!.Address!.City == "Oslo").Body.ToString().Should().Be("(o.Customer.Address.City == \"Oslo\")");
    }

    [Fact]
    public void ToSourceFilter_ValueTypeMember_DropsTheGuard()
    {
        ToSourceFilter(v => v.Customer!.Id > 5).Body.ToString().Should().Be("(o.Customer.Id > 5)");
    }

    [Fact]
    public void ToSourceFilter_RenamedMember_ReadsTheSourceMember()
    {
        ToSourceFilter(v => v.Customer!.Code == "A").Body.ToString().Should().Be("(o.Customer.Name == \"A\")");
    }

    [Fact]
    public void ToSourceFilter_ComputedMember_KeepsTheGuard()
    {
        // `Name ?? "none"` is not null when the customer is missing, so its guard has to stay
        var filter = ToSourceFilter(v => v.Customer!.Label == "none").Compile();

        filter(new Order()).Should().BeFalse();
        filter(new Order { Customer = new Customer() }).Should().BeTrue();
        filter(new Order { Customer = new Customer { Name = "Ann" } }).Should().BeFalse();
    }

    [Fact]
    public void ToSourceFilter_MixedFilter_DropsOnlyTheGuardsThatMakeNoDifference()
    {
        var filter = ToSourceFilter(v => v.Customer!.Address!.City == "Oslo" && v.Customer!.Label == "none");

        // the city is read directly, the computed label keeps its guard
        filter.Body.ToString().Should().StartWith("((o.Customer.Address.City == \"Oslo\") AndAlso (IIF(");
        filter.Compile()(new Order { Customer = new Customer { Address = new Address { City = "Oslo" } } }).Should().BeTrue();
        filter.Compile()(new Order { Customer = new Customer { Name = "Ann", Address = new Address { City = "Oslo" } } }).Should().BeFalse();
    }

    [Fact]
    public void ToSourceFilter_NullCheckOfProjectedObject_TestsTheGuardedMember()
    {
        ToSourceFilter(v => v.Customer == null).Body.ToString().Should().Be("(o.Customer == null)");
    }

    [Fact]
    public void ToSourceFilter_NullCheckOfNestedProjectedObject_TestsEveryGuard()
    {
        var filter = ToSourceFilter(v => v.Customer!.Address != null).Compile();

        filter(new Order()).Should().BeFalse();
        filter(new Order { Customer = new Customer() }).Should().BeFalse();
        filter(new Order { Customer = new Customer { Address = new Address() } }).Should().BeTrue();
    }

    [Fact]
    public void ToSourceFilter_CollectionOperator_ReadsTheProjectedSequence()
    {
        var filter = ToSourceFilter(v => v.Lines!.Any(l => l.Sku == "A"));

        filter.Body.ToString().Should().NotContain("ToList");
        filter.Compile()(new Order { Lines = [new Line { Sku = "A" }] }).Should().BeTrue();
        filter.Compile()(new Order { Lines = [new Line { Sku = "B" }] }).Should().BeFalse();
    }

    [Fact]
    public void Apply_ProjectedQuery_FiltersTheSource()
    {
        var query = FilterPushdownVisitor.Apply(new List<Order>().AsQueryable().Select(Projection), v => v.Number == "Q1");

        CallsOf(query).Should().Equal(nameof(Queryable.Select), nameof(Queryable.Where));
    }

    [Fact]
    public void Apply_QueryWithoutProjection_FiltersItAsIs()
    {
        var query = FilterPushdownVisitor.Apply(new List<OrderView>().AsQueryable(), v => v.Number == "Q1");

        CallsOf(query).Should().Equal(nameof(Queryable.Where));
    }

    [Fact]
    public void Transform_ByDefault_FiltersTheProjectedView()
    {
        var query = Rql.Transform(new List<Order>().AsQueryable(), new RqlRequest { Filter = "eq(number,Q1)" }).Query;

        CallsOf(query).Should().Equal(nameof(Queryable.Where), nameof(Queryable.Select));
    }

    [Fact]
    public void Transform_CollectionQuantifierWithScopeWithoutOptimization_NormalizesThePredicate()
    {
        var source = new List<Order>().AsQueryable();
        var request = new RqlRequest
        {
            Filter = "any(lines,eq(sku,A))"
        };

        var response = Rql.Transform(
            source,
            request,
            options =>
            {
                options.Settings.Mapping.Transparent = false;
                options.Settings.Filter.Optimization = FilterOptimization.None;
                options.Settings.Filter.Navigation = NavigationStrategy.Default;
                options.Settings.Mapping.Navigation = NavigationStrategy.Default;

                options.ScopeCollection<Order, Line>(
                    order => order.Lines,
                    line => line.Sku == "A");
            });

        response.IsSuccess.Should().BeTrue();

        CallsOf(response.Query)
            .Should()
            .Equal(
                nameof(Queryable.Select),
                nameof(Queryable.Where),
                nameof(Queryable.Select));

        var resultSelect =
            (MethodCallExpression)response.Query.Expression;
        var where =
            (MethodCallExpression)resultSelect.Arguments[0];
        var predicate =
            (LambdaExpression)((UnaryExpression)where.Arguments[1]).Operand;

        predicate.Parameters.Should().ContainSingle()
            .Which.Type.Should().Be(
                typeof(CollectionPredicateRow<Order, OrderView>));

        predicate.Body.ToString()
            .Should().Contain("row.Source.Lines")
            .And.NotContain("row.View")
            .And.NotContain("ToList")
            .And.NotContain("Select(");
    }

    [Theory]
    [InlineData("any(lines,eq(sku,A))")]
    [InlineData("all(lines,eq(sku,A))")]
    [InlineData("and(eq(number,Q1),any(lines,eq(sku,A)))")]
    public void Transform_CollectionFilterWithoutScopes_KeepsTheProjectedFilter(
        string filter)
    {
        var source = new List<Order>().AsQueryable();
        var request = new RqlRequest
        {
            Filter = filter
        };

        var response = Rql.Transform(
            source,
            request,
            options =>
            {
                options.Settings.Mapping.Transparent = false;
                options.Settings.Filter.Optimization = FilterOptimization.None;
                options.Settings.Filter.Navigation = NavigationStrategy.Default;
                options.Settings.Mapping.Navigation = NavigationStrategy.Default;
            });

        response.IsSuccess.Should().BeTrue(
            string.Join("; ", response.Errors.Select(error => error.Message)));

        CallsOf(response.Query)
            .Should()
            .Equal(nameof(Queryable.Where), nameof(Queryable.Select));

        var where = (MethodCallExpression)response.Query.Expression;
        var predicate =
            (LambdaExpression)((UnaryExpression)where.Arguments[1]).Operand;

        predicate.Parameters.Should().ContainSingle()
            .Which.Type.Should().Be(typeof(OrderView));
    }

    [Fact]
    public void Transform_Optimization_FiltersTheSource()
    {
        var query = Rql.Transform(new List<Order>().AsQueryable(), new RqlRequest { Filter = "eq(number,Q1)" },
            options => options.Settings.Filter.Optimization = FilterOptimization.RemoveRedundantNullChecks).Query;

        CallsOf(query).Should().Equal(nameof(Queryable.Select), nameof(Queryable.Where));
    }

    [Fact]
    public void Transform_OptimizationWithSafeFilterNavigation_FiltersTheProjectedView()
    {
        var query = Rql.Transform(new List<Order>().AsQueryable(), new RqlRequest { Filter = "eq(number,Q1)" }, options =>
        {
            options.Settings.Filter.Optimization = FilterOptimization.RemoveRedundantNullChecks;
            options.Settings.Filter.Navigation = NavigationStrategy.Safe;
        }).Query;

        CallsOf(query).Should().Equal(nameof(Queryable.Where), nameof(Queryable.Select));
    }

    [Fact]
    public void Transform_OptimizationWithSafeMappingNavigation_FiltersTheProjectedView()
    {
        var query = Rql.Transform(new List<Order>().AsQueryable(), new RqlRequest { Filter = "eq(number,Q1)" }, options =>
        {
            options.Settings.Filter.Optimization = FilterOptimization.RemoveRedundantNullChecks;
            options.Settings.Mapping.Navigation = NavigationStrategy.Safe;
        }).Query;

        CallsOf(query).Should().Equal(nameof(Queryable.Where), nameof(Queryable.Select));
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("A", true)]
    [InlineData("B", false)]
    public void ToSourceFilter_WhenGuardRemovalIsDisabled_PreservesSafeReferenceFilter(
        string? name,
        bool expected)
    {
        Expression<Func<OrderView, bool>> filter =
            view => view.Customer != null && view.Customer.Code == "A";

        var source = new Order
        {
            Customer = new Customer { Name = name }
        };

        var rewritten = (Expression<Func<Order, bool>>)
            FilterPushdownVisitor.ToSourceFilter(
                filter,
                Projection,
                removeRedundantNullChecks: false);

        var result = rewritten.Compile()(source);

        result.Should().Be(expected);
        rewritten.Compile()(new Order()).Should().BeFalse();
        rewritten.Body.ToString().Should().Contain("IIF");
    }

    [Theory]
    [InlineData(
        FilterOptimization.None,
        NavigationStrategy.Default,
        NavigationStrategy.Default)]
    [InlineData(
        FilterOptimization.None,
        NavigationStrategy.Safe,
        NavigationStrategy.Default)]
    [InlineData(
        FilterOptimization.None,
        NavigationStrategy.Default,
        NavigationStrategy.Safe)]
    [InlineData(
        FilterOptimization.None,
        NavigationStrategy.Safe,
        NavigationStrategy.Safe)]
    [InlineData(
        FilterOptimization.RemoveRedundantNullChecks,
        NavigationStrategy.Safe,
        NavigationStrategy.Default)]
    [InlineData(
        FilterOptimization.RemoveRedundantNullChecks,
        NavigationStrategy.Default,
        NavigationStrategy.Safe)]
    [InlineData(
        FilterOptimization.RemoveRedundantNullChecks,
        NavigationStrategy.Safe,
        NavigationStrategy.Safe)]
    public void Transform_ScalarFilterWithScope_FiltersTheProjectedView(
        FilterOptimization optimization,
        NavigationStrategy filterNavigation,
        NavigationStrategy mappingNavigation)
    {
        using var services = new ServiceCollection()
            .AddRql(options =>
            {
                options.Settings.Mapping.Transparent = false;
                options.Settings.Select.Implicit = RqlSelectModes.All;
                options.Settings.Select.Explicit = RqlSelectModes.All;
            })
            .BuildServiceProvider();

        var rql = services.GetRequiredService<
            IRqlQueryable<Order, OrderView>>();

        var source = new List<Order>
        {
            new()
            {
                Number = "Q1",
                Lines =
                [
                    new Line { Sku = "A" },
                    new Line { Sku = "B" }
                ]
            },
            new()
            {
                Number = "Q2",
                Lines = [new Line { Sku = "A" }]
            }
        }.AsQueryable();

        var request = new RqlRequest
        {
            Filter = "eq(number,Q1)"
        };

        var response = rql.Transform(
            source,
            request,
            options =>
            {
                options.Settings.Filter.Optimization = optimization;
                options.Settings.Filter.Navigation = filterNavigation;
                options.Settings.Mapping.Navigation = mappingNavigation;

                options.ScopeCollection<Order, Line>(
                    order => order.Lines,
                    line => line.Sku == "A");
            });

        response.IsSuccess.Should().BeTrue(
            string.Join("; ", response.Errors.Select(error => error.Message)));

        var result = response.Query.ToList();

        CallsOf(response.Query)
            .Should()
            .Equal(nameof(Queryable.Where), nameof(Queryable.Select));

        result.Should().ContainSingle()
            .Which.Number.Should().Be("Q1");

        result[0].Lines.Should().ContainSingle()
            .Which.Sku.Should().Be("A");
    }

    [Fact]
    public void Transform_ScalarFilterWithScopeAndOptimization_FiltersTheSource()
    {
        using var services = new ServiceCollection()
            .AddRql(options =>
            {
                options.Settings.Mapping.Transparent = false;
            })
            .BuildServiceProvider();

        var rql = services.GetRequiredService<
            IRqlQueryable<Order, OrderView>>();

        var source = new List<Order>().AsQueryable();
        var request = new RqlRequest
        {
            Filter = "eq(number,Q1)"
        };

        var response = rql.Transform(
            source,
            request,
            options =>
            {
                options.Settings.Filter.Optimization =
                    FilterOptimization.RemoveRedundantNullChecks;
                options.Settings.Filter.Navigation = NavigationStrategy.Default;
                options.Settings.Mapping.Navigation = NavigationStrategy.Default;

                options.ScopeCollection<Order, Line>(
                    order => order.Lines,
                    line => line.Sku == "A");
            });

        response.IsSuccess.Should().BeTrue(
            string.Join("; ", response.Errors.Select(error => error.Message)));

        CallsOf(response.Query)
            .Should()
            .Equal(nameof(Queryable.Select), nameof(Queryable.Where));
    }

    [Theory]
    [InlineData(
        "and(eq(number,Q1),any(lines,eq(sku,A)))",
        new string[] { "Q1" })]
    [InlineData(
        "or(eq(number,Q1),any(lines,eq(sku,A)))",
        new string[] { "Q1", "Q1", "Q2" })]
    public void Transform_MixedFilterWithScope_KeepsScalarPredicateOnTheView(
        string filter,
        string[] expectedNumbers)
    {
        var source = new List<Order>
        {
            new()
            {
                Number = "Q1",
                Lines = [new Line { Sku = "A" }]
            },
            new()
            {
                Number = "Q1",
                Lines = [new Line { Sku = "B" }]
            },
            new()
            {
                Number = "Q2",
                Lines = [new Line { Sku = "A" }]
            }
        }.AsQueryable();

        var request = new RqlRequest
        {
            Filter = filter
        };

        var response = Rql.Transform(
            source,
            request,
            options =>
            {
                options.Settings.Mapping.Transparent = false;
                options.Settings.Filter.Optimization = FilterOptimization.None;
                options.Settings.Filter.Navigation = NavigationStrategy.Default;
                options.Settings.Mapping.Navigation = NavigationStrategy.Default;

                options.ScopeCollection<Order, Line>(
                    order => order.Lines,
                    line => line.Sku == "A");
            });

        response.IsSuccess.Should().BeTrue(
            string.Join("; ", response.Errors.Select(error => error.Message)));

        var result = response.Query.ToList();

        CallsOf(response.Query)
            .Should()
            .Equal(
                nameof(Queryable.Select),
                nameof(Queryable.Where),
                nameof(Queryable.Select));

        var resultSelect =
            (MethodCallExpression)response.Query.Expression;
        var where =
            (MethodCallExpression)resultSelect.Arguments[0];
        var predicate =
            (LambdaExpression)((UnaryExpression)where.Arguments[1]).Operand;

        predicate.Body.ToString()
            .Should().Contain("row.View.Number")
            .And.Contain("row.Source.Lines")
            .And.NotContain("row.Source.Number")
            .And.NotContain("row.View.Lines")
            .And.NotContain("ToList")
            .And.NotContain("Select(");

        result.Select(order => order.Number)
            .Should().Equal(expectedNumbers);
    }

    // the query operators from the outermost in
    private static List<string> CallsOf(IQueryable query)
    {
        var calls = new List<string>();
        for (var expression = query.Expression; expression is MethodCallExpression call; expression = call.Arguments[0])
            calls.Add(call.Method.Name);

        return calls;
    }

    private static Expression<Func<Order, bool>> ToSourceFilter(Expression<Func<OrderView, bool>> filter)
        => (Expression<Func<Order, bool>>)FilterPushdownVisitor.ToSourceFilter(filter, Projection);

    internal class Order
    {
        public string Number { get; set; } = null!;
        public Customer? Customer { get; set; }
        public List<Line> Lines { get; set; } = [];
    }

    internal class Customer
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public Address? Address { get; set; }
    }

    internal class Address
    {
        public string City { get; set; } = null!;
    }

    internal class Line
    {
        public string Sku { get; set; } = null!;
    }

    internal class OrderView
    {
        public string Number { get; set; } = null!;
        public CustomerView? Customer { get; set; }
        public List<LineView>? Lines { get; set; }
    }

    internal class CustomerView
    {
        public int Id { get; set; }
        public string? Code { get; set; }
        public string Label { get; set; } = null!;
        public AddressView? Address { get; set; }
    }

    internal class AddressView
    {
        public string City { get; set; } = null!;
    }

    internal class LineView
    {
        public string Sku { get; set; } = null!;
    }
}
