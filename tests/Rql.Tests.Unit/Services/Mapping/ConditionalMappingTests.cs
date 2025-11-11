using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Mpt.Rql;
using Mpt.Rql.Services.Mapping;
using Xunit;

namespace Rql.Tests.Unit.Services.Mapping;
public class ConditionalMappingTests
{
    [Fact]
    public void MapConditional_WithComplexObject_MapsCorrectly()
    {
        // Arrange, Act, Assert
        var sp = MakeServiceProvider(t =>
        {
            t.AddScoped(typeof(IRqlMapper<DbPost, Post>), typeof(PostConditionalMapper));
        });
        var rql = sp.GetRequiredService<IRqlQueryable<DbPost, Post>>();
        var data = new List<DbPost>
        {
            new () { Type = 1, User = new() { Name = "User Name", Contact = new () { Name = "Contact Name" } }},
            new () { Type = 2, User = new() { Name = "User Name", Contact = new () { Name = "Contact Name" } }},
            new () { Type = 3, User = new() { Name = "User Name", Contact = new () { Name = "Contact Name" } }},
        };

        var transformed = rql.Transform(data.AsQueryable(), new RqlRequest());
        var result = transformed.Query.ToList();
        result.Should().HaveCount(3);

        result[0].User.Name.Should().Be("User Name");
        result[1].User.Name.Should().Be("Contact Name");
        result[2].User.Name.Should().Be("Default Name");
    }

    [Fact]
    public void MapConditional_WithSimpleObject_MapsCorrectly()
    {
        // Arrange, Act, Assert
        var sp = MakeServiceProvider(t =>
        {
            t.AddScoped(typeof(IRqlMapper<DbUser, User>), typeof(UserConditionalMapper));
        });
        var rql = sp.GetRequiredService<IRqlQueryable<DbUser, User>>();
        var data = new List<DbUser>
        {
            new() { Name = "User Name", Type = 1, Contact = new () { Name = "Contact Name" } },
            new() { Name = "User Name", Type = 2, Contact = new () { Name = "Contact Name" } },
            new() { Name = "User Name", Type = 3, Contact = new () { Name = "Contact Name" } },
        };

        var transformed = rql.Transform(data.AsQueryable(), new RqlRequest());
        var result = transformed.Query.ToList();
        result.Should().HaveCount(3);

        result[0].Name.Should().Be("User Name");
        result[1].Name.Should().Be("Contact Name");
        result[2].Name.Should().Be("Default Name");
    }

    [Fact]
    public void RegisterMapping_WithInvalidConditionalMap_ThrowsException()
    {
        // Arrange, Act, Assert
        var sp = MakeServiceProvider(t =>
        {
            t.AddScoped(typeof(IRqlMapper<DbPost, Post>), typeof(IncorrectPostConditionalMapper));
        });

        var rql = sp.GetRequiredService<IRqlQueryable<DbPost, Post>>();
        var data = new List<DbPost>();
        var exception = Assert.Throws<RqlMappingException>(() => rql.Transform(data.AsQueryable(), new RqlRequest()));
        Assert.Equal("Switch mapping for property 'user' must have default case.", exception.Message);
    }

    private static ServiceProvider MakeServiceProvider(Action<ServiceCollection> configure)
    {
        var services = new ServiceCollection();
        services.AddRql(t =>
        {
            t.Settings.Select.Implicit = t.Settings.Select.Explicit = RqlSelectModes.All;
        });
        configure(services);
        return services.BuildServiceProvider();
    }

    internal class Post
    {
        public User User { get; set; } = null!;
    }

    internal class User
    {
        public string Name { get; set; } = null!;
    }

    internal class DbPost
    {
        public DbUser User { get; set; } = null!;

        public int Type { get; set; }
    }

    internal class DbUser
    {
        public string Name { get; set; } = null!;

        public DbContact Contact { get; set; } = null!;

        public int Type { get; set; }
    }

    internal class DbContact
    {
        public string Name { get; set; } = null!;
    }

    internal class PostConditionalMapper : IRqlMapper<DbPost, Post>
    {
        public void MapEntity(IRqlMapperContext<DbPost, Post> context)
        {
            context.Switch(t => t.User)
                .Case(t => t.Type == 1, t => t.User)
                .Case(t => t.Type == 2, t => t.User.Contact)
                .Default(t => new User { Name = "Default Name" }, true);
        }
    }

    internal class UserConditionalMapper : IRqlMapper<DbUser, User>
    {
        public void MapEntity(IRqlMapperContext<DbUser, User> context)
        {
            context.Switch(t => t.Name)
                .Case(t => t.Type == 1, t => t.Name)
                .Case(t => t.Type == 2, t => t.Contact.Name)
                .Default(t => "Default Name", true);
        }
    }

    internal class IncorrectPostConditionalMapper : IRqlMapper<DbPost, Post>
    {
        public void MapEntity(IRqlMapperContext<DbPost, Post> context)
        {
            context.Switch(t => t.User)
                .Case(t => t.Type == 1, t => t.User)
                .Case(t => t.Type == 2, t => t.User.Contact);
        }
    }
}
