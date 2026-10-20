using Microsoft.EntityFrameworkCore;
using Mpt.Rql;

namespace Rql.Tests.EfCore;

public class Quote
{
    public int Id { get; set; }
    public string Number { get; set; } = null!;
    public int? SupportCaseId { get; set; }
    public SupportCase? SupportCase { get; set; }
    public List<QuoteLine> Lines { get; set; } = [];
}

public class SupportCase
{
    public int Id { get; set; }
    public string Status { get; set; } = null!;
    public int? ReporterId { get; set; }
    public Contact? Reporter { get; set; }
}

public class Contact
{
    public int Id { get; set; }
    public string Email { get; set; } = null!;
    public string? Phone { get; set; }
}

public class QuoteLine
{
    public int Id { get; set; }
    public int QuoteId { get; set; }
    public string Sku { get; set; } = null!;
}

public class QuoteView
{
    [RqlProperty(IsCore = true)]
    public int Id { get; set; }

    public string Number { get; set; } = null!;

    public SupportCaseView? SupportCase { get; set; }

    [RqlProperty(Select = RqlSelectModes.None)]
    public List<QuoteLineView>? Lines { get; set; }
}

public class SupportCaseView
{
    public int Id { get; set; }
    public string Status { get; set; } = null!;
    public ContactView? Reporter { get; set; }
}

public class ContactView
{
    public int Id { get; set; }
    public string Email { get; set; } = null!;
    public string? Phone { get; set; }
}

public class QuoteLineView
{
    public int Id { get; set; }
    public string Sku { get; set; } = null!;
}

public class QuotesDbContext(DbContextOptions<QuotesDbContext> options) : DbContext(options)
{
    public DbSet<Quote> Quotes => Set<Quote>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Quote>().HasOne(q => q.SupportCase).WithMany().HasForeignKey(q => q.SupportCaseId);
        modelBuilder.Entity<Quote>().HasMany(q => q.Lines).WithOne().HasForeignKey(l => l.QuoteId);
        modelBuilder.Entity<SupportCase>().HasOne(c => c.Reporter).WithMany().HasForeignKey(c => c.ReporterId);
    }
}
