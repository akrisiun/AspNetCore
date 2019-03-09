using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using WebApiSample.DataAccess.Repositories;

#region snippet_ApiControllerAttributeOnAssembly
[assembly: ApiController]
namespace WebApiSample.Api._22
{
    public class Startup
    {
        #endregion snippet_ApiControllerAttributeOnAssembly
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            #region snippet_ConfigureApiBehaviorOptions

            services.AddMvcApi()
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_2)
                .ConfigureApiBehaviorOptions(options =>
                {
                    // https://docs.microsoft.com/en-us/aspnet/core/web-api/?view=aspnetcore-2.2#annotate-class-with-apicontrollerattribute
                    // Disabling the default behavior is useful when your action can recover from a model validation error.
                    options.SuppressConsumesConstraintForFormFileParameters = true;
                    options.SuppressInferBindingSourcesForParameters = true;
                    options.SuppressModelStateInvalidFilter = true;
                    // if transform certain certain client errors.
                    options.SuppressMapClientErrors = true;

                    options.ClientErrorMapping[404].Link = 
                        "https://httpstatuses.com/404";
                });

            #endregion

            // IServiceProvider Services
            services.AddSingleton<ProductsRepository>();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();

                Console.WriteLine("http://localhost:5000/api/products");
            } 
			else {
                // app.UseHsts();
            }

            // app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}
