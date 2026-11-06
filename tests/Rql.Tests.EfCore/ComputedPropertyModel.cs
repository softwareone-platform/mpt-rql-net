using Microsoft.EntityFrameworkCore;
using Mpt.Rql;

namespace Rql.Tests.EfCore;

public class ComputedSubscription
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public List<ComputedSubscriptionParameter> Parameters { get; set; } = [];
}

public class ComputedSubscriptionParameter
{
    public int Id { get; set; }
    public int SubscriptionId { get; set; }
    public string ExternalId { get; set; } = null!;
    public string? RawValue { get; set; }
    public string? Format { get; set; }

    // the unrelated large column a computed property must not pull in
    public string? DisplayValue { get; set; }
}

public class ComputedSubscriptionView
{
    [RqlProperty(IsCore = true)]
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public List<ComputedParameterView>? Parameters { get; set; }
}

public class ComputedParameterView
{
    public string ExternalId { get; set; } = null!;

    public string? DisplayValue { get; set; }

    [RqlProperty(Actions = RqlActions.None)]
    public string? RawValue { get; set; }

    [RqlProperty(Actions = RqlActions.None)]
    public string? Format { get; set; }

    // computed from the two inputs above, so it has no source of its own
    [RqlProperty(Actions = RqlActions.Select, DependsOn = new[] { nameof(RawValue), nameof(Format) })]
    public string? Value => Format == "upper" ? RawValue?.ToUpperInvariant() : RawValue;
}

public class ComputedSubscriptionsDbContext(DbContextOptions<ComputedSubscriptionsDbContext> options) : DbContext(options)
{
    public DbSet<ComputedSubscription> Subscriptions => Set<ComputedSubscription>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ComputedSubscription>().HasMany(s => s.Parameters).WithOne().HasForeignKey(p => p.SubscriptionId);
    }
}
