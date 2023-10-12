using Rql.Sample.Application;
using Rql.Sample.Infrastructure;

namespace Rql.Sample.Api
{
    public static class Program
    {
        private const string CorsPolicyName = "__cors_policy";

        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services
                .AddPresentation()
                .AddApplication()
                .AddInfrastructure();

            builder.Services.AddCors(options =>
            {
                options.AddPolicy(name: CorsPolicyName, b => b.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
            });


            var app = builder.Build();
            app.UseCors(CorsPolicyName);
            app.UseSwagger();
            app.UseSwaggerUI();
            app.UseAuthorization();
            app.MapControllers();
            app.UseExceptionHandler("/error");
            app.Run();
        }
    }
}