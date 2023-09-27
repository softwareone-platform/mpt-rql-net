using Rql.Sample.Application;
using Rql.Sample.Infrastructure;

namespace Rql.Sample.Api
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services
                .AddPresentation()
                .AddApplication()
                .AddInfrastructure();

            var app = builder.Build();
            app.UseSwagger();
            app.UseSwaggerUI();
            app.UseAuthorization();
            app.MapControllers();
            app.UseExceptionHandler("/error");
            app.Run();
        }
    }
}