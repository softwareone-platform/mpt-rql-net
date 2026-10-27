using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mpt.Rql;
using Mpt.Rql.Abstractions.Configuration;
using Mpt.Rql.Abstractions.Configuration.Filter;
using Xunit;

namespace Rql.Tests.EfCore;

public sealed class CollectionScopeFilterTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ScopeDbContext _context;
    private readonly ServiceProvider _services;
    private readonly IRqlQueryable<ContactEntity, ContactView> _rql;

    public CollectionScopeFilterTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var dbOptions = new DbContextOptionsBuilder<ScopeDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new ScopeDbContext(dbOptions);
        _context.Database.EnsureCreated();

        Seed();

        _services = new ServiceCollection()
            .AddRql(options =>
            {
                options.Settings.Mapping.Transparent = false;
                options.Settings.Select.Implicit = RqlSelectModes.All;
                options.Settings.Select.Explicit = RqlSelectModes.All;
                options.Settings.Select.MaxDepth = 5;
            })
            .BuildServiceProvider();

        _rql = _services
            .GetRequiredService<IRqlQueryable<ContactEntity, ContactView>>();
    }

    public static TheoryData<FilterOptimization, NavigationStrategy> Modes =>
        new()
        {
            { FilterOptimization.None, NavigationStrategy.Default },
            {
                FilterOptimization.RemoveRedundantNullChecks,
                NavigationStrategy.Default
            },
            { FilterOptimization.None, NavigationStrategy.Safe },
            {
                FilterOptimization.RemoveRedundantNullChecks,
                NavigationStrategy.Safe
            }
        };

    public static TheoryData<
        string,
        int[],
        FilterOptimization,
        NavigationStrategy> FilterCases
    {
        get
        {
            var cases = new (string Filter, int[] ExpectedIds)[]
            {
                ("any(directories,eq(name,a))", [2]),
                ("not(any(directories,eq(name,a)))", [1, 3, 4, 5]),
                ("not(any(directories))", [3, 4]),
                ("any(directories)", [1, 2, 5]),
                ("all(directories,eq(name,b))", [1, 3, 4, 5])
            };

            var optimizations = new[]
            {
                FilterOptimization.None,
                FilterOptimization.RemoveRedundantNullChecks
            };

            var navigationModes = new[]
            {
                NavigationStrategy.Default,
                NavigationStrategy.Safe
            };

            var data = new TheoryData<
                string,
                int[],
                FilterOptimization,
                NavigationStrategy>();

            foreach (var testCase in cases)
            {
                foreach (var optimization in optimizations)
                {
                    foreach (var navigation in navigationModes)
                    {
                        data.Add(
                            testCase.Filter,
                            testCase.ExpectedIds,
                            optimization,
                            navigation);
                    }
                }
            }

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(FilterCases))]
    public async Task Filter_UsesScopedCollection_WhenSelectingParents(
        string filter,
        int[] expectedIds,
        FilterOptimization optimization,
        NavigationStrategy navigation)
    {
        var request = new RqlRequest
        {
            Filter = filter,
            Order = "id"
        };

        var query = Transform(request, optimization, navigation);

        var result = await query.ToListAsync();

        result.Select(contact => contact.Id).Should().Equal(expectedIds);

        result
            .SelectMany(contact => contact.Directories)
            .Where(directory =>
                directory.Id != 12 &&
                directory.Id != 21 &&
                directory.Id != 52)
            .Should()
            .BeEmpty();
    }

    [Theory]
    [MemberData(nameof(Modes))]
    public async Task Projection_ReturnsOnlyVisibleItems_WithoutRemovingParents(
        FilterOptimization optimization,
        NavigationStrategy navigation)
    {
        var request = new RqlRequest { Order = "id" };
        var query = Transform(request, optimization, navigation);

        var result = await query.ToListAsync();

        result.Select(contact => contact.Id).Should().Equal(1, 2, 3, 4, 5);

        result[0].Directories.Select(directory => directory.Id)
            .Should().Equal(12);

        result[1].Directories.Select(directory => directory.Id)
            .Should().Equal(21);

        result[2].Directories.Should().NotBeNull().And.BeEmpty();
        result[3].Directories.Should().NotBeNull().And.BeEmpty();

        result[4].Directories.Select(directory => directory.Id)
            .Should().Equal(52);
    }

    [Theory]
    [MemberData(nameof(Modes))]
    public async Task Filter_WhenAppliedBeforePaging_DoesNotReturnHiddenMatch(
        FilterOptimization optimization,
        NavigationStrategy navigation)
    {
        var request = new RqlRequest
        {
            Filter = "any(directories,eq(name,a))",
            Order = "id"
        };

        var query = Transform(request, optimization, navigation);

        var result = await query.Take(1).ToListAsync();

        result.Should().ContainSingle();
        result[0].Id.Should().Be(2);
        result[0].Directories.Select(directory => directory.Id)
            .Should().Equal(21);
    }

    [Theory]
    [MemberData(nameof(Modes))]
    public async Task Count_UsesScopedCollection_WhenCountingParents(
        FilterOptimization optimization,
        NavigationStrategy navigation)
    {
        var request = new RqlRequest
        {
            Filter = "any(directories,eq(name,a))"
        };

        var query = Transform(request, optimization, navigation);

        var count = await query.CountAsync();

        count.Should().Be(1);
    }
    
    [Theory]
    [InlineData(
        FilterOptimization.None,
        NavigationStrategy.Default,
        NavigationStrategy.Safe)]
    [InlineData(
        FilterOptimization.RemoveRedundantNullChecks,
        NavigationStrategy.Default,
        NavigationStrategy.Safe)]
    [InlineData(
        FilterOptimization.None,
        NavigationStrategy.Safe,
        NavigationStrategy.Default)]
    [InlineData(
        FilterOptimization.RemoveRedundantNullChecks,
        NavigationStrategy.Safe,
        NavigationStrategy.Default)]
    public async Task Filter_WithMixedNavigationModes_UsesScopedCollection(
        FilterOptimization optimization,
        NavigationStrategy filterNavigation,
        NavigationStrategy mappingNavigation)
    {
        var request = new RqlRequest
        {
            Filter = "any(directories,eq(name,a))",
            Order = "id"
        };

        var query = Transform(
            request,
            optimization,
            filterNavigation,
            mappingNavigation);

        var result = await query.ToListAsync();

        result.Select(contact => contact.Id).Should().Equal(2);
        result[0].Directories.Select(directory => directory.Id)
            .Should().Equal(21);
    }
    
    [Theory]
    [MemberData(nameof(Modes))]
    public async Task Filter_WhenOnlyIdIsSelected_StillUsesCollectionScope(
        FilterOptimization optimization,
        NavigationStrategy navigation)
    {
        var request = new RqlRequest
        {
            Select = "id",
            Filter = "any(directories,eq(name,a))",
            Order = "id"
        };

        var response = _rql.Transform(
            _context.Contacts.AsNoTracking(),
            request,
            options =>
            {
                options.Settings.Select.Implicit = RqlSelectModes.None;
                options.Settings.Select.Explicit = RqlSelectModes.None;
                options.Settings.Filter.Optimization = optimization;
                options.Settings.Filter.Navigation = navigation;
                options.Settings.Mapping.Navigation = navigation;

                options.ScopeCollection<ContactEntity, DirectoryEntity>(
                    contact => contact.Directories,
                    directory => directory.IsVisible && directory.TenantId == 7);
            });

        var result = await response.Query.ToListAsync();

        response.IsSuccess.Should().BeTrue(
            string.Join("; ", response.Errors.Select(error => error.Message)));

        result.Select(contact => contact.Id).Should().Equal(2);
    }

    [Theory]
    [MemberData(nameof(Modes))]
    public async Task Filter_ThroughNestedCollection_IgnoresInvisibleDirectories(
        FilterOptimization optimization,
        NavigationStrategy navigation)
    {
        var request = new RqlRequest
        {
            Filter = "any(directories,any(optOuts,eq(category,marketing)))",
            Order = "id"
        };

        var query = Transform(request, optimization, navigation);

        var result = await query.ToListAsync();

        result.Select(contact => contact.Id).Should().Equal(2);
        result[0].Directories.Select(directory => directory.Id)
            .Should().Equal(21);

        result[0].Directories
            .SelectMany(directory => directory.OptOuts)
            .Select(optOut => optOut.Id)
            .Should().Equal(102);
    }
    
    private IQueryable<ContactView> Transform(
        RqlRequest request,
        FilterOptimization optimization,
        NavigationStrategy navigation,
        NavigationStrategy? mappingNavigation = null)
    {
        var currentTenantId = 7;

        var response = _rql.Transform(
            _context.Contacts.AsNoTracking(),
            request,
            options =>
            {
                options.Settings.Filter.Optimization = optimization;
                options.Settings.Filter.Navigation = navigation;
                options.Settings.Mapping.Navigation = mappingNavigation ?? navigation;

                options.ScopeCollection<ContactEntity, DirectoryEntity>(
                    contact => contact.Directories,
                    directory =>
                        directory.IsVisible &&
                        directory.TenantId == currentTenantId);
            });

        response.IsSuccess.Should().BeTrue(
            string.Join("; ", response.Errors.Select(error => error.Message)));

        return response.Query;
    }

    private void Seed()
    {
        _context.Contacts.AddRange(
            new ContactEntity
            {
                Id = 1,
                Directories =
                [
                    new DirectoryEntity
                    {
                        Id = 11,
                        Name = "a",
                        IsVisible = false,
                        TenantId = 7
                    },
                    new DirectoryEntity
                    {
                        Id = 12,
                        Name = "b",
                        IsVisible = true,
                        TenantId = 7
                    }
                ]
            },
            new ContactEntity
            {
                Id = 2,
                Directories =
                [
                    new DirectoryEntity
                    {
                        Id = 21,
                        Name = "a",
                        IsVisible = true,
                        TenantId = 7
                    },
                    new DirectoryEntity
                    {
                        Id = 22,
                        Name = "c",
                        IsVisible = false,
                        TenantId = 7
                    }
                ]
            },
            new ContactEntity
            {
                Id = 3,
                Directories =
                [
                    new DirectoryEntity
                    {
                        Id = 31,
                        Name = "a",
                        IsVisible = false,
                        TenantId = 7
                    }
                ]
            },
            new ContactEntity
            {
                Id = 4
            },
            new ContactEntity
            {
                Id = 5,
                Directories =
                [
                    new DirectoryEntity
                    {
                        Id = 51,
                        Name = "a",
                        IsVisible = true,
                        TenantId = 8
                    },
                    new DirectoryEntity
                    {
                        Id = 52,
                        Name = "b",
                        IsVisible = true,
                        TenantId = 7
                    }
                ]
            });

        _context.SaveChanges();
        _context.Set<OptOutEntity>().AddRange(
            new OptOutEntity
            {
                Id = 101,
                DirectoryId = 11,
                Category = "marketing"
            },
            new OptOutEntity
            {
                Id = 102,
                DirectoryId = 21,
                Category = "marketing"
            },
            new OptOutEntity
            {
                Id = 103,
                DirectoryId = 51,
                Category = "marketing"
            },
            new OptOutEntity
            {
                Id = 104,
                DirectoryId = 52,
                Category = "billing"
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

    public sealed class ScopeDbContext(
        DbContextOptions<ScopeDbContext> options) : DbContext(options)
    {
        public DbSet<ContactEntity> Contacts => Set<ContactEntity>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ContactEntity>()
                .HasKey(contact => contact.Id);

            modelBuilder.Entity<DirectoryEntity>()
                .HasMany(directory => directory.OptOuts)
                .WithOne()
                .HasForeignKey(optOut => optOut.DirectoryId);

            modelBuilder.Entity<ContactEntity>()
                .HasMany(contact => contact.Directories)
                .WithOne()
                .HasForeignKey(directory => directory.ContactId);
        }
    }

    public sealed class ContactEntity
    {
        public int Id { get; set; }
        public List<DirectoryEntity> Directories { get; set; } = [];
    }

    public sealed class DirectoryEntity
    {
        public int Id { get; set; }
        public int ContactId { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsVisible { get; set; }
        public int TenantId { get; set; }
        public List<OptOutEntity> OptOuts { get; set; } = [];
    }

    public sealed class ContactView
    {
        [RqlProperty(IsCore = true)]
        public int Id { get; set; }

        [RqlProperty(IsCore = true)]
        public List<DirectoryView> Directories { get; set; } = [];
    }

    public sealed class DirectoryView
    {
        [RqlProperty(IsCore = true)]
        public int Id { get; set; }

        [RqlProperty(IsCore = true)]
        public string Name { get; set; } = string.Empty;
        
        [RqlProperty(IsCore = true)]
        public List<OptOutView> OptOuts { get; set; } = [];
    }
    
    public sealed class OptOutEntity
    {
        public int Id { get; set; }
        public int DirectoryId { get; set; }
        public string Category { get; set; } = string.Empty;
    }

    public sealed class OptOutView
    {
        [RqlProperty(IsCore = true)]
        public int Id { get; set; }

        [RqlProperty(IsCore = true)]
        public string Category { get; set; } = string.Empty;
    }
}