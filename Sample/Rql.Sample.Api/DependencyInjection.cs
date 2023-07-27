namespace Rql.Sample.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddPresentation(this IServiceCollection services)
    {
        services.AddControllers().AddJsonOptions(json =>
        {
            json.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault;
            json.JsonSerializerOptions.WriteIndented = true;
        });

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        services.AddSoftwareOneRql(t =>
        {
            t.ScanForMappings(typeof(Program).Assembly);
        });
        return services;
    }
}