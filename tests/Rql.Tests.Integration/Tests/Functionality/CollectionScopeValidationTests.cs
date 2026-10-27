using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Mpt.Rql;
using Xunit;

namespace Rql.Tests.Integration.Tests.Functionality;

public sealed class CollectionScopeValidationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Transform_ScopeForDifferentStorageType_ThrowsDuringConfiguration(
        bool transparentMapping)
    {
        using var services = CreateServices(transparentMapping);
        var rql = services.GetRequiredService<
            IRqlQueryable<SourceEntity, SourceEntity>>();

        var source = Array.Empty<SourceEntity>().AsQueryable();
        var request = new RqlRequest();

        Action act = () => rql.Transform(
            source,
            request,
            options => options.ScopeCollection<OtherSourceEntity, LineEntity>(
                entity => entity.Lines,
                line => line.IsVisible));

        act.Should().ThrowExactly<ArgumentException>()
            .WithParameterName("configure")
            .WithMessage(
                $"*'{typeof(OtherSourceEntity)}'*" +
                $"*'{typeof(SourceEntity)}'*");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void BuildGraph_ScopeForDifferentStorageType_ThrowsDuringConfiguration(
        bool transparentMapping)
    {
        using var services = CreateServices(transparentMapping);
        var rql = services.GetRequiredService<
            IRqlQueryable<SourceEntity, SourceEntity>>();

        var request = new RqlRequest();

        Action act = () => rql.BuildGraph(
            request,
            options => options.ScopeCollection<OtherSourceEntity, LineEntity>(
                entity => entity.Lines,
                line => line.IsVisible));

        act.Should().ThrowExactly<ArgumentException>()
            .WithParameterName("configure")
            .WithMessage(
                $"*'{typeof(OtherSourceEntity)}'*" +
                $"*'{typeof(SourceEntity)}'*");
    }

    private static ServiceProvider CreateServices(bool transparentMapping)
    {
        return new ServiceCollection()
            .AddRql(options =>
            {
                options.Settings.Mapping.Transparent = transparentMapping;
            })
            .BuildServiceProvider();
    }

    public sealed class SourceEntity
    {
        public int Id { get; set; }
        public List<LineEntity> Lines { get; set; } = [];
    }

    public sealed class OtherSourceEntity
    {
        public List<LineEntity> Lines { get; set; } = [];
    }

    public sealed class LineEntity
    {
        public int Id { get; set; }
        public bool IsVisible { get; set; }
    }
}