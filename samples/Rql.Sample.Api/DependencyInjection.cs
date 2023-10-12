using Microsoft.Extensions.Options;
using Rql.Sample.Api.Extensions.Core;
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
        services.AddHttpContextAccessor();

        services.AddRql(t =>
        {
            t.ScanForMappers(typeof(Program).Assembly);

            t.Settings.DefaultActions = RqlActions.All;

            var selectSettings = t.Settings.Select;
            selectSettings.Mode = SelectMode.All;
            selectSettings.MaxDepth = 1;
        });

        return services;
    }
}