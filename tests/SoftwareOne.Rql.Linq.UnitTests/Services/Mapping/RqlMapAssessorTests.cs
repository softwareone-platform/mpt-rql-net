using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace SoftwareOne.Rql.Linq.UnitTests.Services.Mapping;
public class RqlMapAssessorTests
{
    private IServiceProvider _serviceProvider;

    public RqlMapAssessorTests()
    {
        var services = new ServiceCollection();
        services.AddRql(t =>
        {
            t.ScanForMappers(typeof(RqlMapAssessorTests).Assembly);
        });

        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public void Get_WhenCalledForExistingMap_ReturnsMap()
    {
        // Arrange, Act
        var accessor = _serviceProvider.GetRequiredService<IRqlMapAccessor>();
        var map = accessor.Get<MapAssessorTestDbEntity, MapAssessorTestEntity>();

        // Assert
        map.GetProperties().Should().HaveCount(2);
        map.TryGetMapByTargetPath("DsplayName", out var _).Should().BeTrue();
    }
}

public class MapAssessorTestEntity
{
    public string Id { get; set; } = null!;

    public string DsplayName { get; set; } = null!;
}

public class MapAssessorTestDbEntity
{
    public string Id { get; set; } = null!;

    public string Name { get; set; } = null!;
}

public class MapAssessorTestMapper : IRqlMapper<MapAssessorTestDbEntity, MapAssessorTestEntity>
{
    public void MapEntity(IRqlMapperContext<MapAssessorTestDbEntity, MapAssessorTestEntity> context)
    {
        context.Map(t => t.DsplayName, t => t.Name);
    }
}
