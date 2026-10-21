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
