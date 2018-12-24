namespace Pushover
{
    using Microsoft.AspNetCore.Builder;

    public static class ApplicationBuilderExtension
    {
        public static IApplicationBuilder UseSwagger(this IApplicationBuilder applicationBuilder, string componentName)
        {
            return applicationBuilder.UseSwagger(c => { c.RouteTemplate = "api-docs/{documentName}/swagger.json"; })
                                     .UseSwaggerUI(
                                         c =>
                                         {

                                             c.SwaggerEndpoint("/api-docs/v1/swagger.json", $"Pushover {componentName} API V1");
                                             c.RoutePrefix = "api-docs";
                                             c.InjectJavascript("/swagger");
                                         });
        }
    }
}
