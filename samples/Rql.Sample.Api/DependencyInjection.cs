using Rql.Sample.Api.Extensions.Core;
using Rql.Sample.Contracts.Ef.Products;
using SoftwareOne.Rql;
using System.Text.Json.Serialization;

namespace Rql.Sample.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddPresentation(this IServiceCollection services)
    {
        services.AddControllers().AddJsonOptions(json =>
        {
            json.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault;
            json.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
            json.JsonSerializerOptions.WriteIndented = true;
        });

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        services.AddScoped<IErrorResultProvider, ErrorResultProvider>();
        services.AddScoped(typeof(IRqlRequest<>), typeof(RqlRequest<>));
        services.AddScoped(typeof(IRqlRequest<,>), typeof(RqlRequest<,>));
        services.AddScoped<SelectNoneStrategy, SelectNoneStrategy>();
        services.AddHttpContextAccessor();

        services.AddRql(t =>
        {
            t.ScanForMappers(typeof(Program).Assembly);
            t.Settings.General.DefaultActions = RqlActions.All;
            t.Settings.Select.Implicit = RqlSelectModes.Core | RqlSelectModes.Primitive;
            t.Settings.Select.Explicit = RqlSelectModes.All;
        });

        return services;
    }
}