using Mpt.Rql;

namespace Rql.Tests.Integration.Core;

/// <summary>Storage side of a mapped entity with nullable nested references, for filter-binding tests.</summary>
public class Ticket
{
    public int Id { get; set; }

    public string? Title { get; set; }

    public int? Score { get; set; }

    public TicketCase? Case { get; set; }

    public TicketPerson? AssignedTo { get; set; }

    public List<TicketTag> Tags { get; set; } = [];
}

public class TicketCase
{
    public int Id { get; set; }

    public string Status { get; set; } = null!;

    public TicketPerson? Reporter { get; set; }
}

public class TicketPerson
{
    public int Id { get; set; }

    public string Email { get; set; } = null!;

    public string? Phone { get; set; }
}

public class TicketTag
{
    public string Value { get; set; } = null!;
}

public class TicketView : ITestEntity
{
    [RqlProperty(IsCore = true)]
    public int Id { get; set; }

    public string? Title { get; set; }

    public int? Score { get; set; }

    public TicketCaseView? Case { get; set; }

    /// <summary>Mapped from <see cref="Ticket.AssignedTo"/>.</summary>
    public TicketPersonView? Owner { get; set; }

    public List<TicketTagView>? Tags { get; set; }
}

public class TicketCaseView
{
    public int Id { get; set; }

    public string Status { get; set; } = null!;

    public TicketPersonView? Reporter { get; set; }
}

public class TicketPersonView
{
    public int Id { get; set; }

    public string Email { get; set; } = null!;

    public string? Phone { get; set; }
}

public class TicketTagView
{
    public string Value { get; set; } = null!;
}

internal class TicketViewMapper : IRqlMapper<Ticket, TicketView>
{
    public void MapEntity(IRqlMapperContext<Ticket, TicketView> context)
    {
        context.MapDynamic(t => t.Owner, t => t.AssignedTo);
    }
}

public static class TicketRepository
{
    public static IQueryable<Ticket> Query() => new List<Ticket>
    {
        new() { Id = 1, Title = "alpha", Score = 5,
            Case = new TicketCase { Id = 10, Status = "Open", Reporter = new TicketPerson { Id = 100, Email = "ann@x.io", Phone = "1" } },
            AssignedTo = new TicketPerson { Id = 500, Email = "owner@x.io" },
            Tags = [new TicketTag { Value = "red" }, new TicketTag { Value = "blue" }] },
        new() { Id = 2, Title = "beta", Score = null,
            Case = new TicketCase { Id = 20, Status = "Closed", Reporter = new TicketPerson { Id = 200, Email = "bob@x.io", Phone = null } },
            AssignedTo = null,
            Tags = [new TicketTag { Value = "red" }] },
        new() { Id = 3, Title = "gamma", Score = 7,
            Case = new TicketCase { Id = 30, Status = "Open", Reporter = null },
            AssignedTo = new TicketPerson { Id = 600, Email = "other@x.io" },
            Tags = [] },
        new() { Id = 4, Title = null, Score = 1,
            Case = null,
            AssignedTo = null,
            Tags = [new TicketTag { Value = "blue" }] },
    }.AsQueryable();
}
