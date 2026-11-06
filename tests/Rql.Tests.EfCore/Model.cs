using Microsoft.EntityFrameworkCore;
using Mpt.Rql;

namespace Rql.Tests.EfCore;

public class Order
{
    public int Id { get; set; }
    public string Number { get; set; } = null!;
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }
    public List<OrderLine> Lines { get; set; } = [];
}

public class Customer
{
    public int Id { get; set; }
    public string Status { get; set; } = null!;
    public int? ContactId { get; set; }
    public Person? Contact { get; set; }
}

public class Person
{
    public int Id { get; set; }
    public string Email { get; set; } = null!;
    public string? Phone { get; set; }
    public int? EmployerId { get; set; }
    public Employer? Employer { get; set; }
}

public class Employer
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
}

public class OrderLine
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string Sku { get; set; } = null!;
}

public class OrderView
{
    [RqlProperty(IsCore = true)]
    public int Id { get; set; }

    public string Number { get; set; } = null!;

    public CustomerView? Customer { get; set; }

    public List<OrderLineView>? Lines { get; set; }
}

public class CustomerView
{
    public int Id { get; set; }
    public string Status { get; set; } = null!;
    public PersonView? Contact { get; set; }
}

public class PersonView
{
    public int Id { get; set; }
    public string Email { get; set; } = null!;
    public string? Phone { get; set; }

    // computed: not null even when the person is missing, unless the projection's guard is kept
    public string Label { get; set; } = null!;

    // renamed: reads the Phone column
    public string? Mobile { get; set; }

    // a deeper path through the person: reads Employer.Name
    public string? EmployerName { get; set; }
}

public class OrderLineView
{
    public int Id { get; set; }
    public string Sku { get; set; } = null!;
}

public class PersonViewMapper : IRqlMapper<Person, PersonView>
{
    public void MapEntity(IRqlMapperContext<Person, PersonView> context)
        => context
            .MapStatic(t => t.Label, t => t.Phone ?? "none")
            .MapStatic(t => t.Mobile, t => t.Phone)
            .MapStatic(t => t.EmployerName, t => t.Employer!.Name);
}

public class OrdersDbContext(DbContextOptions<OrdersDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>().HasOne(o => o.Customer).WithMany().HasForeignKey(o => o.CustomerId);
        modelBuilder.Entity<Order>().HasMany(o => o.Lines).WithOne().HasForeignKey(l => l.OrderId);
        modelBuilder.Entity<Customer>().HasOne(c => c.Contact).WithMany().HasForeignKey(c => c.ContactId);
        modelBuilder.Entity<Person>().HasOne(p => p.Employer).WithMany().HasForeignKey(p => p.EmployerId);
    }
}
