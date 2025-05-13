using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using SoftwareOne.Rql.Linq.Services.Mapping;
using Xunit;

namespace SoftwareOne.Rql.Linq.UnitTests.Services.Mapping;
public class ConditionalMappingTests
{
    [Fact]
    public void MapConditional_NameProperty_MapsBasedOnTypeCorrectly()
    {
        // Arrange, Act, Assert
        var sp = MakeServiceProvider(t =>
        {
            t.AddScoped(typeof(IRqlMapper<DbUser, User>), typeof(ConditionalMapper));
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
            t.AddScoped(typeof(IRqlMapper<DbUser, User>), typeof(IncorrectConditionalMapper));
        });

        var rql = sp.GetRequiredService<IRqlQueryable<DbUser, User>>();
        var data = new List<DbUser>();
        var exception = Assert.Throws<RqlMappingException>(() => rql.Transform(data.AsQueryable(), new RqlRequest()));
        Assert.Equal("'Else' expression cannot be empty.", exception.Message);
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

    internal class User
    {
        public string Name { get; set; } = null!;
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

    internal class ConditionalMapper : IRqlMapper<DbUser, User>
    {
        public void MapEntity(IRqlMapperContext<DbUser, User> context)
        {
            context.MapConditional(t => t.Name, c =>
            {
                c.If(t => t.Type == 1, t => t.Name);
                c.If(t => t.Type == 2, t => t.Contact.Name);
                c.Else(t => "Default Name");
            });
        }
    }

    internal class IncorrectConditionalMapper : IRqlMapper<DbUser, User>
    {
        public void MapEntity(IRqlMapperContext<DbUser, User> context)
        {
            context.MapConditional(t => t.Name, c =>
            {
                c.If(t => t.Type == 1, t => t.Name);
                c.If(t => t.Type == 2, t => t.Contact.Name);
            });
        }
    }
}
