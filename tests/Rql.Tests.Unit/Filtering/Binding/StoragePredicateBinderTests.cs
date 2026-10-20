using FluentAssertions;
using Mpt.Rql.Core.Expressions;
using Mpt.Rql.Services.Filtering.Binding;
using System.Linq.Expressions;
using Xunit;

namespace Rql.Tests.Unit.Filtering.Binding;

/// <summary>
/// Shapes are the ones RQL produces: the projection guards nullable references with
/// <c>x != null ? new T { ... } : null</c>, safe navigation guards path segments with <c>x == null ? null : x.M</c>.
/// Each test checks the rewritten shape and, where the view predicate itself is null-safe, that both forms select
/// the same rows under LINQ-to-Objects.
/// </summary>
public class StoragePredicateBinderTests
{
    private static readonly Expression<Func<Ticket, TicketView>> _selector = t => new TicketView
    {
        Id = t.Id,
        Title = t.Title,
        Score = t.Score,
        Case = t.Case != null
            ? new CaseView
            {
                Id = t.Case.Id,
                Status = t.Case.Status,
                Reporter = t.Case.Reporter != null
                    ? new PersonView { Id = t.Case.Reporter.Id, Email = t.Case.Reporter.Email, Phone = t.Case.Reporter.Phone }
                    : null
            }
            : null,
        Tags = t.Tags.Select(g => new TagView { Value = g.Value }).ToList()
    };

    private static readonly Ticket[] _tickets =
    [
        new() { Id = 1, Title = "a", Score = 5, Case = new TicketCase { Id = 10, Status = "Open", Reporter = new Person { Id = 100, Email = "ann", Phone = "1" } }, Tags = [new TicketTag { Value = "t1" }] },
        new() { Id = 2, Title = "b", Score = null, Case = new TicketCase { Id = 20, Status = "Closed", Reporter = new Person { Id = 200, Email = "bob", Phone = null } } },
        new() { Id = 3, Title = "c", Score = 7, Case = new TicketCase { Id = 30, Status = "Open", Reporter = null } },
        new() { Id = 4, Title = null, Score = 1, Case = null },
    ];

    [Fact]
    public void Eq_ThroughTwoNullableReferences_BecomesConjunctionWithoutConditional()
    {
        var bound = Bind(v => v.Case!.Reporter!.Email == "ann");

        bound.Body.ToString().Should().Be("(((t.Case != null) AndAlso (t.Case.Reporter != null)) AndAlso (t.Case.Reporter.Email == \"ann\"))");
        Ids(bound).Should().Equal(1);
    }

    [Fact]
    public void Ne_ThroughNullableReferences_IsTrueWhenAGuardFails()
    {
        var bound = Bind(v => v.Case!.Reporter!.Email != "ann");

        bound.Body.ToString().Should().Be("(((t.Case == null) OrElse (t.Case.Reporter == null)) OrElse (t.Case.Reporter.Email != \"ann\"))");
        Ids(bound).Should().Equal(2, 3, 4);
    }

    [Fact]
    public void EqNull_ThroughNullableReferences_IsTrueWhenAGuardFailsOrTheLeafIsNull()
    {
        var bound = Bind(v => v.Case!.Reporter!.Phone == null);

        bound.Body.ToString().Should().Be("(((t.Case == null) OrElse (t.Case.Reporter == null)) OrElse (t.Case.Reporter.Phone == null))");
        Ids(bound).Should().Equal(2, 3, 4);
    }

    [Fact]
    public void NeNull_ThroughNullableReferences_RequiresEveryGuardAndTheLeaf()
    {
        var bound = Bind(v => v.Case!.Reporter!.Phone != null);

        bound.Body.ToString().Should().Be("(((t.Case != null) AndAlso (t.Case.Reporter != null)) AndAlso (t.Case.Reporter.Phone != null))");
        Ids(bound).Should().Equal(1);
    }

    [Fact]
    public void EqNull_OnAGuardedReference_IsTheNegatedGuard()
    {
        var bound = Bind(v => v.Case!.Reporter == null);

        bound.Body.ToString().Should().Be("((t.Case == null) OrElse (t.Case.Reporter == null))");
        Ids(bound).Should().Equal(3, 4);
        AssertSameRows(v => v.Case == null || v.Case.Reporter == null, bound);
    }

    [Fact]
    public void Relational_OnAValueTypeMember_UndoesTheLiftAndKeepsTheGuard()
    {
        var bound = Bind(v => v.Case!.Id > 15);

        bound.Body.ToString().Should().Be("((t.Case != null) AndAlso (t.Case.Id > 15))");
        Ids(bound).Should().Equal(2, 3);
    }

    [Fact]
    public void Eq_OnAValueTypeMember_UndoesTheLift()
    {
        var bound = Bind(v => v.Case!.Id == 10);

        bound.Body.ToString().Should().Be("((t.Case != null) AndAlso (t.Case.Id == 10))");
        Ids(bound).Should().Equal(1);
    }

    [Fact]
    public void InstanceMethod_OnAGuardedString_IsCalledUnderTheGuard()
    {
        var bound = Bind(v => v.Case!.Reporter!.Email.StartsWith("an"));

        bound.Body.ToString().Should().Be("(((t.Case != null) AndAlso (t.Case.Reporter != null)) AndAlso t.Case.Reporter.Email.StartsWith(\"an\"))");
        Ids(bound).Should().Equal(1);
    }

    [Fact]
    public void NegatedInstanceMethod_IsTrueWhenAGuardFails()
    {
        var bound = Bind(v => !v.Case!.Reporter!.Email.Equals("ann"));

        bound.Body.ToString().Should().Be("Not((((t.Case != null) AndAlso (t.Case.Reporter != null)) AndAlso t.Case.Reporter.Email.Equals(\"ann\")))");
        Ids(bound).Should().Equal(2, 3, 4);
    }

    [Fact]
    public void ListContains_OnAGuardedMember_IsCalledUnderTheGuard()
    {
        // RQL's in() calls Enumerable.Contains on a hosted list
        var values = new List<string> { "ann", "bob" };
        var bound = Bind(v => Enumerable.Contains(values, v.Case!.Reporter!.Email));

        bound.Body.ToString().Should().EndWith(".Contains(t.Case.Reporter.Email))").And.StartWith("(((t.Case != null) AndAlso (t.Case.Reporter != null)) AndAlso ");
        Ids(bound).Should().Equal(1, 2);
    }

    [Fact]
    public void ListContains_OnAGuardedValueTypeMember_UndoesTheLift()
    {
        // in(case.id,(10,20)): the list is List<int>, the hoisted member was lifted to int?
        var values = new List<int> { 10, 20 };
        var bound = Bind(v => Enumerable.Contains(values, v.Case!.Id));

        bound.Body.ToString().Should().EndWith(".Contains(t.Case.Id))").And.StartWith("((t.Case != null) AndAlso ");
        Ids(bound).Should().Equal(1, 2);
    }

    [Fact]
    public void NullChecksOnDifferentParameters_AreBothKept()
    {
        // unnamed parameters print alike; de-duplication must key on the parameter instance
        var t = Expression.Parameter(typeof(Ticket));
        var other = Expression.Parameter(typeof(Ticket));
        var left = Expression.NotEqual(Expression.Property(t, nameof(Ticket.Case)), Expression.Constant(null, typeof(TicketCase)));
        var right = Expression.NotEqual(Expression.Property(other, nameof(Ticket.Case)), Expression.Constant(null, typeof(TicketCase)));
        var selector = Expression.Lambda<Func<Ticket, TicketView>>(Expression.MemberInit(Expression.New(typeof(TicketView))), t);
        var predicate = Expression.Lambda<Func<TicketView, bool>>(Expression.AndAlso(left, right), Expression.Parameter(typeof(TicketView)));

        var bound = StoragePredicateBinder.Bind(predicate, selector);

        ((BinaryExpression)bound.Body).NodeType.Should().Be(ExpressionType.AndAlso);
        ((BinaryExpression)bound.Body).Left.Should().BeSameAs(left);
        ((BinaryExpression)bound.Body).Right.Should().BeSameAs(right);
    }

    [Fact]
    public void SafeNavigationGuards_CollapseIntoTheProjectionGuards()
    {
        // Filter.Navigation = Safe: the path builder guards every segment on the view side
        var bound = Bind(v => (v.Case == null ? null : v.Case.Reporter == null ? null : v.Case.Reporter.Email) == "ann");

        bound.Body.ToString().Should().Be("(((t.Case != null) AndAlso (t.Case.Reporter != null)) AndAlso (t.Case.Reporter.Email == \"ann\"))");
        AssertSameRows(v => (v.Case == null ? null : v.Case.Reporter == null ? null : v.Case.Reporter.Email) == "ann", bound);
    }

    [Fact]
    public void SafeNavigation_WithOperatorNullSafety_StaysEquivalent()
    {
        // WithNullSafetyIfEnabled: accessor != null && accessor.StartsWith(...)
        Expression<Func<TicketView, bool>> view = v =>
            (v.Case == null ? null : v.Case.Reporter == null ? null : v.Case.Reporter.Email) != null
            && (v.Case == null ? null : v.Case.Reporter == null ? null : v.Case.Reporter.Email)!.StartsWith("an");

        var bound = Bind(view);

        HasConditional(bound).Should().BeFalse();
        AssertSameRows(view, bound);
    }

    [Fact]
    public void Any_OverAProjectedCollection_BindsThePredicateToTheElementProjection()
    {
        var bound = Bind(v => v.Tags!.Any(g => g.Value == "t1"));

        bound.Body.ToString().Should().Be("t.Tags.Any(g => (g.Value == \"t1\"))");
        Ids(bound).Should().Equal(1);
        AssertSameRows(v => v.Tags!.Any(g => g.Value == "t1"), bound);
    }

    [Fact]
    public void Any_WithoutPredicate_OverAProjectedCollection_DropsTheProjection()
    {
        var bound = Bind(v => v.Tags!.Any());

        bound.Body.ToString().Should().Be("t.Tags.Any()");
        AssertSameRows(v => v.Tags!.Any(), bound);
    }

    [Fact]
    public void Any_OverAGuardedCollection_IsCalledUnderTheGuard()
    {
        // Mapping.Navigation = Safe guards the collection too
        Expression<Func<Ticket, TicketView>> safeSelector = t => new TicketView
        {
            Id = t.Id,
            Tags = t.Tags != null ? t.Tags.Select(g => new TagView { Value = g.Value }).ToList() : null
        };

        var bound = StoragePredicateBinder.Bind(v => v.Tags != null && v.Tags.Any(g => g.Value == "t1"), safeSelector);

        // the operator's own null check and the projection guard are the same term, kept once
        bound.Body.ToString().Should().Be("((t.Tags != null) AndAlso t.Tags.Any(g => (g.Value == \"t1\")))");
        Ids(bound).Should().Equal(1);
    }

    [Fact]
    public void Eq_AgainstANullableOperand_AccountsForTheOperandBeingNull()
    {
        // self(title): when the guard fails the comparison is null == title
        var bound = Bind(v => v.Case!.Reporter!.Email == v.Title);

        bound.Body.ToString().Should().Be(
            "((((t.Case != null) AndAlso (t.Case.Reporter != null)) AndAlso (t.Case.Reporter.Email == t.Title)) OrElse (((t.Case == null) OrElse (t.Case.Reporter == null)) AndAlso (t.Title == null)))");
        Ids(bound).Should().Equal(4);
    }

    [Fact]
    public void Eq_WithGuardsOnBothSides_IsLeftUntouched()
    {
        Expression<Func<TicketView, bool>> view = v => v.Case!.Reporter!.Email == v.Case!.Status;

        var bound = Bind(view);

        HasConditional(bound).Should().BeTrue();
        bound.Compile().Invoke(_tickets[0]).Should().BeFalse();
    }

    [Fact]
    public void ConditionalWithTwoValues_IsLeftUntouched()
    {
        // MapIf: both branches are values, so this is not a null guard
        Expression<Func<Ticket, TicketView>> selector = t => new TicketView
        {
            Id = t.Id,
            Title = t.Score > 3 ? t.Title : "low"
        };

        var bound = StoragePredicateBinder.Bind(v => v.Title == "low", selector);

        bound.Body.ToString().Should().Be("(IIF((t.Score > Convert(3, Nullable`1)), t.Title, \"low\") == \"low\")");
        _tickets.AsQueryable().Where(bound).Select(t => t.Id).Should().Equal(2, 4);
    }

    [Fact]
    public void MemberNotBoundByTheProjection_IsLeftToTheProvider()
    {
        Expression<Func<Ticket, TicketView>> selector = t => new TicketView { Id = t.Id };

        var bound = StoragePredicateBinder.Bind(v => v.Title == "a", selector);

        bound.Body.ToString().Should().Be("(new TicketView() {Id = t.Id}.Title == \"a\")");
    }

    [Fact]
    public void HostedConstantsThatPrintAlike_AreNotCollapsed()
    {
        // RQL hosts values in HostedConstant<T>.Value, whose printed form does not show the value; only null checks
        // may be de-duplicated. and(eq(id,1),eq(id,2)) must stay two terms (and match nothing).
        var one = ConstantBuilder.Build(1, typeof(int));
        var two = ConstantBuilder.Build(2, typeof(int));
        var v = Expression.Parameter(typeof(TicketView), "v");
        var id = Expression.Property(v, nameof(TicketView.Id));
        var predicate = Expression.Lambda<Func<TicketView, bool>>(
            Expression.AndAlso(Expression.AndAlso(Expression.Equal(id, one), Expression.Equal(id, two)), Expression.Equal(Expression.Property(v, nameof(TicketView.Title)), Expression.Constant("a"))),
            v);

        var bound = StoragePredicateBinder.Bind(predicate, _selector);

        bound.Body.ToString().Should().Be("(((t.Id == value(Mpt.Rql.Core.Expressions.HostedConstant`1[System.Int32]).Value) AndAlso (t.Id == value(Mpt.Rql.Core.Expressions.HostedConstant`1[System.Int32]).Value)) AndAlso (t.Title == \"a\"))");
        Ids(bound).Should().BeEmpty();
    }

    [Fact]
    public void RepeatedNullChecks_AreKeptOnce()
    {
        // the projection guard and the operator's own null check are the same term
        var bound = Bind(v => v.Case != null && v.Case.Id > 15);

        bound.Body.ToString().Should().Be("((t.Case != null) AndAlso (t.Case.Id > 15))");
        Ids(bound).Should().Equal(2, 3);
    }

    [Fact]
    public void UnguardedPredicate_IsBoundToTheProjectionOnly()
    {
        var bound = Bind(v => v.Title == "a" && v.Score > 3);

        bound.Body.ToString().Should().Be("((t.Title == \"a\") AndAlso (t.Score > Convert(3, Nullable`1)))");
        Ids(bound).Should().Equal(1);
    }

    private static Expression<Func<Ticket, bool>> Bind(Expression<Func<TicketView, bool>> predicate)
        => StoragePredicateBinder.Bind(predicate, _selector);

    private static List<int> Ids(Expression<Func<Ticket, bool>> bound)
        => _tickets.AsQueryable().Where(bound).Select(t => t.Id).ToList();

    private static void AssertSameRows(Expression<Func<TicketView, bool>> view, Expression<Func<Ticket, bool>> bound)
    {
        var expected = _tickets.AsQueryable().Select(_selector).Where(view).Select(v => v.Id).ToList();
        Ids(bound).Should().Equal(expected);
    }

    private static bool HasConditional(Expression expression)
    {
        var finder = new ConditionalFinder();
        finder.Visit(expression);
        return finder.Found;
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

    public class Ticket
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public int? Score { get; set; }
        public TicketCase? Case { get; set; }
        public List<TicketTag> Tags { get; set; } = [];
    }

    public class TicketCase
    {
        public int Id { get; set; }
        public string Status { get; set; } = null!;
        public Person? Reporter { get; set; }
    }

    public class Person
    {
        public int Id { get; set; }
        public string Email { get; set; } = null!;
        public string? Phone { get; set; }
    }

    public class TicketTag
    {
        public string Value { get; set; } = null!;
    }

    public class TicketView
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public int? Score { get; set; }
        public CaseView? Case { get; set; }
        public List<TagView>? Tags { get; set; }
    }

    public class CaseView
    {
        public int Id { get; set; }
        public string Status { get; set; } = null!;
        public PersonView? Reporter { get; set; }
    }

    public class PersonView
    {
        public int Id { get; set; }
        public string Email { get; set; } = null!;
        public string? Phone { get; set; }
    }

    public class TagView
    {
        public string Value { get; set; } = null!;
    }
}
