using System.Linq.Expressions;
using FluentAssertions;
using Mpt.Rql.Core;
using Xunit;

namespace Rql.Tests.Unit.Core;

public sealed class CollectionQueryHelperTests
{
    public static TheoryData<Type, Type[]> EnumerableTypes => new()
    {
        { typeof(IEnumerable<int>), [typeof(int)] },
        { typeof(ICollection<int>), [typeof(int)] },
        { typeof(List<int>), [typeof(int)] },
        { typeof(int[]), [typeof(int)] },
        { typeof(object), [] },
        { typeof(string), [typeof(char)] },
        { typeof(IMultipleElementTypes), [typeof(int), typeof(string)] }
    };

    public static TheoryData<Expression<Func<IEnumerable<int>, bool>>, bool>
        EnumerableQuantifiers => new()
        {
            { values => values.Any(), true },
            { values => values.Any(value => value > 0), true },
            { values => values.All(value => value > 0), true },
            { values => values.Contains(1), false }
        };

    public static TheoryData<Expression<Func<Owner, bool>>, bool>
        GuardedQuantifiers => new()
        {
            {
                owner => owner.Values != null && owner.Values.Any(),
                true
            },
            {
                owner => null != owner.Values &&
                    owner.Values.All(value => value > 0),
                true
            },
            {
                owner => owner.Values != null &&
                    owner.OtherValues!.Any(),
                false
            },
            {
                owner => owner.Values == null &&
                    owner.Values!.Any(),
                false
            },
            {
                owner => owner.Values != null ||
                    owner.Values!.Any(),
                false
            },
            {
                owner => owner.Values != null &&
                    owner.Values.Contains(1),
                false
            }
        };

    [Theory]
    [MemberData(nameof(EnumerableTypes))]
    public void GetEnumerableElementTypes_ReturnsDistinctExposedTypes(
        Type collectionType,
        Type[] expectedTypes)
    {
        var result =
            CollectionQueryHelper.GetEnumerableElementTypes(collectionType);

        result.Should().BeEquivalentTo(expectedTypes);
        result.Should().OnlyHaveUniqueItems();
    }

    [Theory]
    [MemberData(nameof(EnumerableQuantifiers))]
    public void IsQuantifier_RecognizesOnlySupportedEnumerableMethods(
        Expression<Func<IEnumerable<int>, bool>> expression,
        bool expected)
    {
        var call = (MethodCallExpression)expression.Body;

        var result = CollectionQueryHelper.IsQuantifier(call);

        result.Should().Be(expected);
    }

    [Fact]
    public void IsQuantifier_QueryableMethod_ReturnsFalse()
    {
        Expression<Func<IQueryable<int>, bool>> expression =
            values => values.Any(value => value > 0);
        var call = (MethodCallExpression)expression.Body;

        var result = CollectionQueryHelper.IsQuantifier(call);

        result.Should().BeFalse();
    }

    [Theory]
    [MemberData(nameof(GuardedQuantifiers))]
    public void IsGuardedQuantifier_RequiresMatchingCollectionNullGuard(
        Expression<Func<Owner, bool>> expression,
        bool expected)
    {
        var binary = (BinaryExpression)expression.Body;

        var result = CollectionQueryHelper.IsGuardedQuantifier(binary);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ComparedWithNull_ReturnsTheNonNullOperand(bool nullOnLeft)
    {
        Expression<Func<Owner, bool>> expression = nullOnLeft
            ? owner => null != owner.Values
            : owner => owner.Values != null;
        var comparison = (BinaryExpression)expression.Body;

        var result = CollectionQueryHelper.ComparedWithNull(comparison);

        result.Should().BeAssignableTo<MemberExpression>()
            .Which.Member.Name.Should().Be(nameof(Owner.Values));
    }

    [Fact]
    public void ComparedWithNull_WithoutNullConstant_ReturnsNull()
    {
        Expression<Func<Owner, bool>> expression =
            owner => owner.Values == owner.OtherValues;
        var comparison = (BinaryExpression)expression.Body;

        var result = CollectionQueryHelper.ComparedWithNull(comparison);

        result.Should().BeNull();
    }

    [Fact]
    public void IsSameAccess_SeparateNodesWithSameRootAndMember_ReturnsTrue()
    {
        var parameter = Expression.Parameter(typeof(Owner), "owner");
        var first = Expression.Property(parameter, nameof(Owner.Values));
        var second = Expression.Property(parameter, nameof(Owner.Values));

        var result = CollectionQueryHelper.IsSameAccess(first, second);

        result.Should().BeTrue();
    }

    [Fact]
    public void IsSameAccess_DifferentRootsWithSameName_ReturnsFalse()
    {
        var firstParameter = Expression.Parameter(typeof(Owner), "owner");
        var secondParameter = Expression.Parameter(typeof(Owner), "owner");
        var first = Expression.Property(firstParameter, nameof(Owner.Values));
        var second = Expression.Property(secondParameter, nameof(Owner.Values));

        var result = CollectionQueryHelper.IsSameAccess(first, second);

        result.Should().BeFalse();
    }

    [Fact]
    public void IsSameAccess_DifferentMembers_ReturnsFalse()
    {
        var parameter = Expression.Parameter(typeof(Owner), "owner");
        var first = Expression.Property(parameter, nameof(Owner.Values));
        var second = Expression.Property(parameter, nameof(Owner.OtherValues));

        var result = CollectionQueryHelper.IsSameAccess(first, second);

        result.Should().BeFalse();
    }

    [Fact]
    public void IsSameMember_SameDeclaredProperty_ReturnsTrue()
    {
        var first = typeof(Owner).GetProperty(nameof(Owner.Values))!;
        var second = typeof(Owner).GetProperty(nameof(Owner.Values))!;

        var result = CollectionQueryHelper.IsSameMember(first, second);

        result.Should().BeTrue();
    }

    [Fact]
    public void IsSameMember_SameNameOnDifferentTypes_ReturnsFalse()
    {
        var first = typeof(Owner).GetProperty(nameof(Owner.Values))!;
        var second = typeof(OtherOwner).GetProperty(nameof(OtherOwner.Values))!;

        var result = CollectionQueryHelper.IsSameMember(first, second);

        result.Should().BeFalse();
    }

    [Fact]
    public void MethodDefinitions_SelectNonIndexedOverloads()
    {
        Expression<Func<IQueryable<int>, IQueryable<string>>> select =
            values => values.Select(value => value.ToString());
        Expression<Func<IQueryable<int>, IQueryable<int>>> queryableWhere =
            values => values.Where(value => value > 0);
        Expression<Func<IEnumerable<int>, IEnumerable<int>>> enumerableWhere =
            values => values.Where(value => value > 0);

        var selectDefinition = ((MethodCallExpression)select.Body)
            .Method.GetGenericMethodDefinition();
        var queryableWhereDefinition = ((MethodCallExpression)queryableWhere.Body)
            .Method.GetGenericMethodDefinition();
        var enumerableWhereDefinition = ((MethodCallExpression)enumerableWhere.Body)
            .Method.GetGenericMethodDefinition();

        Assert.Equal(selectDefinition, CollectionQueryHelper.QueryableSelect);
        Assert.Equal(queryableWhereDefinition, CollectionQueryHelper.QueryableWhere);
        Assert.Equal(enumerableWhereDefinition, CollectionQueryHelper.EnumerableWhere);
    }

    public interface IMultipleElementTypes :
        IEnumerable<int>,
        IEnumerable<string>
    {
    }

    public sealed class Owner
    {
        public List<int>? Values { get; set; }
        public List<int>? OtherValues { get; set; }
    }

    public sealed class OtherOwner
    {
        public List<int>? Values { get; set; }
    }
}