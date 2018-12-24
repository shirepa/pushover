namespace Pushover
{
    using System.IO;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.PlatformAbstractions;
    using Swashbuckle.AspNetCore.Swagger;
    using Service;
    using Components;

    public static class ServiceCollectionExtension
    {

        public static IServiceCollection AddMessageService(this IServiceCollection services)
        {
            return services.AddScoped<IMessageService, MessageService>();
        }

        public static IServiceCollection AddHttpClient(this IServiceCollection services)
        {
            return services.AddScoped<IHttpClient, ReliableHttpClient>();
        }

        public static IServiceCollection AddSwaggerDocumentation(
         this IServiceCollection serviceCollection,
         string componentName,
         string fileName)
        {
            return serviceCollection.AddSwaggerGen(
                c =>
                {
                    c.SwaggerDoc(
                        "v1",
                        new Info
                        {
                            Title = $"Pushover {componentName} API",
                            Version = "1.0.0",
                            Description = $"API documentation for the Pushover {componentName}"
                        });
                    var filePath = Path.Combine(
                        PlatformServices.Default.Application.ApplicationBasePath,
                        fileName);
                    c.DescribeAllEnumsAsStrings();
                });
        }
    }
}
