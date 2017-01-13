using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using Octokit;
using RadarrAPI.Database;
using RadarrAPI.Release;

namespace RadarrAPI
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", true, true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", true, true)
                .AddEnvironmentVariables();

            Configuration = builder.Build();
            
            // Check data path
            if (!Path.IsPathRooted(Configuration["DataDirectory"]))
            {
                throw new Exception("DataDirectory path must be absolute.");
            }

            // Create
            Directory.CreateDirectory(Configuration["DataDirectory"]);
        }

        public IConfigurationRoot Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<Config>(Configuration);
            services.AddSingleton(new GitHubClient(new ProductHeaderValue("RadarrAPI")));
            services.AddSingleton<ReleaseService>();
            services.AddMvc();

            // Add database
            var sqlite = new SqliteConnectionStringBuilder
            {
                DataSource = Path.Combine(Configuration["DataDirectory"], "radarrapi.db"),
                Mode = SqliteOpenMode.ReadWriteCreate
            };
            
            services.AddDbContext<DatabaseContext>(o => o.UseSqlite(sqlite.ConnectionString));
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddNLog();
            
            env.ConfigureNLog("nlog.config");

            app.UseMvc();
        }
    }
}
