using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using Octokit;

namespace RadarrAPI
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", true, true)
                .AddEnvironmentVariables();

            Configuration = builder.Build();

            // Check config
            if (string.IsNullOrWhiteSpace(Configuration["DataDirectory"]))
                throw new Exception("DataDirectory was not set in the Configuration.");

            // Check data path
            if (!Path.IsPathRooted(Configuration["DataDirectory"]))
            {
                Configuration["DataDirectory"] = Path.GetFullPath(Configuration["DataDirectory"]);
            }

            // Create
            Directory.CreateDirectory(Configuration["DataDirectory"]);
        }

        public IConfigurationRoot Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<Config>(Configuration);
            services.AddMemoryCache();
            services.AddSingleton(new GitHubClient(new ProductHeaderValue("RadarrAPI")));
            services.AddMvc();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddNLog();
            
            env.ConfigureNLog("nlog.config");

            app.UseMvc();
        }
    }
}
