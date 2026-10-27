using System.Linq.Expressions;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mpt.Rql;
using Mpt.Rql.Abstractions.Configuration;
using Mpt.Rql.Abstractions.Configuration.Filter;
using Xunit;

namespace Rql.Tests.EfCore;

public sealed class NullableReferenceCollectionFilterTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly TestDbContext _context;
    private readonly ServiceProvider _services;
    private readonly IRqlQueryable<ParentEntity, ParentView> _rql;

    public NullableReferenceCollectionFilterTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var dbOptions = new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new TestDbContext(dbOptions);
        _context.Database.EnsureCreated();

        Seed();

        _services = new ServiceCollection()
            .AddRql(options =>
            {
                options.Settings.Mapping.Transparent = false;
                options.Settings.Mapping.Navigation = NavigationStrategy.Default;
                options.Settings.Filter.Navigation = NavigationStrategy.Default;
                options.Settings.Select.Implicit = RqlSelectModes.All;
                options.Settings.Select.Explicit = RqlSelectModes.All;
                options.Settings.Select.MaxDepth = 5;
            })
            .AddScoped<
                IRqlMapper<ParentEntity, ConditionalParentView>,
                ConditionalParentMapper>()
            .AddScoped<
                IRqlMapper<ParentEntity, MixedParentView>,
                MixedParentMapper>()
            .AddScoped<ParentBucketFactory>()
            .BuildServiceProvider();

        _rql = _services
            .GetRequiredService<IRqlQueryable<ParentEntity, ParentView>>();
    }

    public static TheoryData<string, int[], FilterOptimization> ScopedFilters
    {
        get
        {
            var cases = new (string Filter, int[] ExpectedIds)[]
            {
                ("any(group.buyers,eq(name,\"a\"))", [3, 6]),
                ("all(group.buyers,eq(name,\"a\"))", [2, 3, 5, 6]),
                ("not(any(group.buyers,eq(name,\"a\")))", [1, 2, 4, 5])
            };

            var optimizations = new[]
            {
                FilterOptimization.None,
                FilterOptimization.RemoveRedundantNullChecks
            };

            var data = new TheoryData<string, int[], FilterOptimization>();

            foreach (var testCase in cases)
            {
                foreach (var optimization in optimizations)
                {
                    data.Add(
                        testCase.Filter,
                        testCase.ExpectedIds,
                        optimization);
                }
            }

            return data;
        }
    }

    public static TheoryData<string, int[], FilterOptimization> UnscopedFilters =>
        new()
        {
            {
                "any(group.buyers,eq(name,\"a\"))",
                [3, 5, 6],
                FilterOptimization.RemoveRedundantNullChecks
            },
            {
                "all(group.buyers,eq(name,\"a\"))",
                [2, 3, 5],
                FilterOptimization.RemoveRedundantNullChecks
            },
            {
                "not(any(group.buyers,eq(name,\"a\")))",
                [1, 2, 4],
                FilterOptimization.RemoveRedundantNullChecks
            }
        };

    [Theory]
    [MemberData(nameof(UnscopedFilters))]
    public async Task Filter_WithoutScopesWithOptimization_SelectsExpectedParents(
        string filter,
        int[] expectedIds,
        FilterOptimization optimization)
    {
        var query = Transform(filter, optimization, applyScope: false);

        var result = await query.ToListAsync();

        result.Select(parent => parent.Id).Should().Equal(expectedIds);
    }

    [Theory]
    [MemberData(nameof(ScopedFilters))]
    public async Task Filter_WithScopeAndDefaultNavigation_SelectsExpectedParents(
        string filter,
        int[] expectedIds,
        FilterOptimization optimization)
    {
        var query = Transform(filter, optimization, applyScope: true);

        var result = await query.ToListAsync();

        result.Select(parent => parent.Id).Should().Equal(expectedIds);

        result
            .Where(parent => parent.Group != null)
            .SelectMany(parent => parent.Group!.Buyers)
            .Where(buyer =>
                buyer.Id != 31 &&
                buyer.Id != 41 &&
                buyer.Id != 61)
            .Should()
            .BeEmpty();
    }

    [Theory]
    [InlineData(FilterOptimization.None)]
    [InlineData(FilterOptimization.RemoveRedundantNullChecks)]
    public async Task Projection_WithScope_PreservesMissingGroupAndEmptyCollections(
        FilterOptimization optimization)
    {
        var query = Transform(null, optimization, applyScope: true);

        var result = await query.ToListAsync();

        result.Select(parent => parent.Id).Should().Equal(1, 2, 3, 4, 5, 6);

        result[0].Group.Should().BeNull();

        result[1].Group.Should().NotBeNull();
        result[1].Group!.Buyers.Should().BeEmpty();

        result[2].Group!.Buyers.Select(buyer => buyer.Id)
            .Should().Equal(31);

        result[3].Group!.Buyers.Select(buyer => buyer.Id)
            .Should().Equal(41);

        result[4].Group.Should().NotBeNull();
        result[4].Group!.Buyers.Should().BeEmpty();

        result[5].Group!.Buyers.Select(buyer => buyer.Id)
            .Should().Equal(61);
    }

    public static TheoryData<string, int[], bool, FilterOptimization>
        ConditionalCollectionFilters
    {
        get
        {
            var cases = new (
                string Filter,
                int[] ExpectedIds,
                bool ApplyScope)[]
                {
                    ("any(buyers,eq(name,\"a\"))", [5, 6], false),
                    ("all(buyers,eq(name,\"a\"))", [2, 5], false),
                    ("not(any(buyers,eq(name,\"a\")))", [1, 2, 3, 4], false),
                    ("any(buyers,eq(name,\"a\"))", [6], true),
                    ("all(buyers,eq(name,\"a\"))", [2, 5, 6], true),
                    ("not(any(buyers,eq(name,\"a\")))", [1, 2, 3, 4, 5], true)
                };

            var optimizations = new[]
            {
                FilterOptimization.None,
                FilterOptimization.RemoveRedundantNullChecks
            };

            var data =
                new TheoryData<string, int[], bool, FilterOptimization>();

            foreach (var testCase in cases)
            {
                foreach (var optimization in optimizations.Where(
                             candidate =>
                                 testCase.ApplyScope ||
                                 candidate == FilterOptimization.RemoveRedundantNullChecks))
                {
                    data.Add(
                        testCase.Filter,
                        testCase.ExpectedIds,
                        testCase.ApplyScope,
                        optimization);
                }
            }

            return data;
        }
    }
    
    [Theory]
    [MemberData(nameof(ConditionalCollectionFilters))]
    public async Task Filter_WithConditionalCollectionMapping_SelectsExpectedParents(
        string filter,
        int[] expectedIds,
        bool applyScope,
        FilterOptimization optimization)
    {
        var rql = _services.GetRequiredService<
            IRqlQueryable<ParentEntity, ConditionalParentView>>();

        var request = new RqlRequest
        {
            Filter = filter,
            Order = "id"
        };

        var response = rql.Transform(
            _context.Parents.AsNoTracking(),
            request,
            options =>
            {
                options.Settings.Filter.Optimization = optimization;

                if (applyScope)
                {
                    options.ScopeCollection<ParentEntity, BuyerEntity>(
                        parent => parent.Group!.Buyers,
                        buyer => buyer.IsVisible);
                }
            });

        response.IsSuccess.Should().BeTrue(
            string.Join("; ", response.Errors.Select(error => error.Message)));

        var result = await response.Query
            .Select(parent => parent.Id)
            .ToListAsync();

        result.Should().Equal(expectedIds);
    }
    
    public static TheoryData<string, int[], bool, FilterOptimization>
        MixedFilters
    {
        get
        {
            var cases = new (
                string Filter,
                int[] ScopedIds,
                int[] UnscopedIds)[]
                {
                    (
                        "and(gt(score,12),any(group.buyers,eq(name,\"a\")))",
                        [3, 6],
                        [3, 5, 6]
                    ),
                    (
                        "or(eq(score,11),any(group.buyers,eq(name,\"a\")))",
                        [1, 3, 6],
                        [1, 3, 5, 6]
                    ),
                    (
                        "and(eq(bucket,0),all(group.buyers,eq(name,\"a\")))",
                        [2, 6],
                        [2]
                    ),
                    (
                        "not(or(eq(bucket,0),any(group.buyers,eq(name,\"a\"))))",
                        [1, 5],
                        [1]
                    )
                };

            var optimizations = new[]
            {
                FilterOptimization.None,
                FilterOptimization.RemoveRedundantNullChecks
            };

            var data =
                new TheoryData<string, int[], bool, FilterOptimization>();

            foreach (var testCase in cases)
            {
                foreach (var optimization in optimizations)
                {
                    data.Add(
                        testCase.Filter,
                        testCase.ScopedIds,
                        true,
                        optimization);
                }

                data.Add(
                    testCase.Filter,
                    testCase.UnscopedIds,
                    false,
                    FilterOptimization.RemoveRedundantNullChecks);
            }

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(MixedFilters))]
    public async Task Filter_WithComputedAndFactoryMembers_TranslatesMixedPredicate(
        string filter,
        int[] expectedIds,
        bool applyScope,
        FilterOptimization optimization)
    {
        var rql = _services.GetRequiredService<
            IRqlQueryable<ParentEntity, MixedParentView>>();

        var request = new RqlRequest
        {
            Filter = filter,
            Order = "id"
        };

        var response = rql.Transform(
            _context.Parents.AsNoTracking(),
            request,
            options =>
            {
                options.Settings.Filter.Optimization = optimization;

                if (applyScope)
                {
                    options.ScopeCollection<ParentEntity, BuyerEntity>(
                        parent => parent.Group!.Buyers,
                        buyer => buyer.IsVisible);
                }
            });

        response.IsSuccess.Should().BeTrue(
            string.Join("; ", response.Errors.Select(error => error.Message)));

        var result = await response.Query.ToListAsync();

        result.Select(parent => parent.Id).Should().Equal(expectedIds);

        result.Select(parent => parent.Score)
            .Should().Equal(expectedIds.Select(id => id + 10));

        result.Select(parent => parent.Bucket)
            .Should().Equal(expectedIds.Select(id => id % 2));
    }

    private IQueryable<ParentView> Transform(
        string? filter,
        FilterOptimization optimization,
        bool applyScope)
    {
        var response = _rql.Transform(
            _context.Parents.AsNoTracking(),
            new RqlRequest
            {
                Filter = filter,
                Order = "id"
            },
            options =>
            {
                options.Settings.Filter.Optimization = optimization;

                if (applyScope)
                {
                    options.ScopeCollection<ParentEntity, BuyerEntity>(
                        parent => parent.Group!.Buyers,
                        buyer => buyer.IsVisible);
                }
            });

        response.IsSuccess.Should().BeTrue(
            string.Join("; ", response.Errors.Select(error => error.Message)));

        return response.Query;
    }

    private void Seed()
    {
        _context.Parents.AddRange(
            new ParentEntity
            {
                Id = 1
            },
            new ParentEntity
            {
                Id = 2,
                Group = new GroupEntity
                {
                    Id = 20
                }
            },
            new ParentEntity
            {
                Id = 3,
                Group = new GroupEntity
                {
                    Id = 30,
                    Buyers =
                    [
                        new BuyerEntity
                        {
                            Id = 31,
                            Name = "a",
                            IsVisible = true
                        }
                    ]
                }
            },
            new ParentEntity
            {
                Id = 4,
                Group = new GroupEntity
                {
                    Id = 40,
                    Buyers =
                    [
                        new BuyerEntity
                        {
                            Id = 41,
                            Name = "b",
                            IsVisible = true
                        }
                    ]
                }
            },
            new ParentEntity
            {
                Id = 5,
                Group = new GroupEntity
                {
                    Id = 50,
                    Buyers =
                    [
                        new BuyerEntity
                        {
                            Id = 51,
                            Name = "a",
                            IsVisible = false
                        }
                    ]
                }
            },
            new ParentEntity
            {
                Id = 6,
                Group = new GroupEntity
                {
                    Id = 60,
                    Buyers =
                    [
                        new BuyerEntity
                        {
                            Id = 61,
                            Name = "a",
                            IsVisible = true
                        },
                        new BuyerEntity
                        {
                            Id = 62,
                            Name = "b",
                            IsVisible = false
                        }
                    ]
                }
            });

        _context.SaveChanges();
        _context.ChangeTracker.Clear();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
        _services.Dispose();
    }

    public sealed class TestDbContext(
        DbContextOptions<TestDbContext> options) : DbContext(options)
    {
        public DbSet<ParentEntity> Parents => Set<ParentEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ParentEntity>()
                .HasOne(parent => parent.Group)
                .WithMany()
                .HasForeignKey(parent => parent.GroupId);

            modelBuilder.Entity<GroupEntity>()
                .HasMany(group => group.Buyers)
                .WithOne()
                .HasForeignKey(buyer => buyer.GroupId);
        }
    }

    public sealed class ParentEntity
    {
        public int Id { get; set; }
        public int? GroupId { get; set; }
        public GroupEntity? Group { get; set; }
    }

    public sealed class GroupEntity
    {
        public int Id { get; set; }
        public List<BuyerEntity> Buyers { get; set; } = [];
    }

    public sealed class BuyerEntity
    {
        public int Id { get; set; }
        public int GroupId { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsVisible { get; set; }
    }

    public sealed class ParentView
    {
        public int Id { get; set; }
        public GroupView? Group { get; set; }
    }

    public sealed class GroupView
    {
        public int Id { get; set; }
        public List<BuyerView> Buyers { get; set; } = [];
    }

    public sealed class BuyerView
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
    
    public sealed class ConditionalParentView
    {
        public int Id { get; set; }
        public List<BuyerView>? Buyers { get; set; }
    }

    public sealed class ConditionalParentMapper
        : IRqlMapper<ParentEntity, ConditionalParentView>
    {
        public void MapEntity(
            IRqlMapperContext<ParentEntity, ConditionalParentView> context)
        {
            context.Switch(parent => parent.Buyers)
                .Case(
                    parent => parent.GroupId != null && parent.Id != 3,
                    parent => parent.Group!.Buyers)
                .Default<List<BuyerView>>(
                    parent => null,
                    mapStatic: true);
        }
    }
    
    public sealed class MixedParentView
    {
        public int Id { get; set; }
        public int Score { get; set; }
        public int Bucket { get; set; }
        public GroupView? Group { get; set; }
    }

    public sealed class MixedParentMapper
        : IRqlMapper<ParentEntity, MixedParentView>
    {
        public void MapEntity(
            IRqlMapperContext<ParentEntity, MixedParentView> context)
        {
            context.MapStatic(
                view => view.Score,
                parent => parent.Id + 10);

            context.MapWithFactory<ParentBucketFactory>(
                view => view.Bucket);
        }
    }

    public sealed class ParentBucketFactory
        : IRqlMappingExpressionFactory<ParentEntity>
    {
        public Expression<Func<ParentEntity, object?>> GetStorageExpression()
            => parent => parent.Id % 2;
    }
}