using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


namespace Pushover
{
    public class Startup
    {

        private const string ComponentName = "PushoverClient";

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {

            AddSwagger(services);
            services.AddHttpClient();
            services.AddMessageService();
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            UseSwagger(app);
            app.UseHttpsRedirection();
            app.UseMvc();
        }

        private void AddSwagger(IServiceCollection serviceCollection)
        {
            serviceCollection.AddSwaggerDocumentation(ComponentName, "Pushover.xml");
        }

        private void UseSwagger(IApplicationBuilder applicationBuilder)
        {
            applicationBuilder.UseSwagger(ComponentName);

        }
    }
}
