using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Octokit;
using RadarrAPI.Database;
using RadarrAPI.Services.BackgroundTasks;
using RadarrAPI.Services.ReleaseCheck;
using RadarrAPI.Services.ReleaseCheck.AppVeyor;
using RadarrAPI.Services.ReleaseCheck.Github;
using RadarrAPI.Services.ReleaseCheck.Azure;
using Serilog;
using TraktApiSharp;
using ProductHeaderValue = Octokit.ProductHeaderValue;

namespace RadarrAPI
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IHostingEnvironment env)
        {
            Config = configuration;
            ConfigRadarr = Config.GetSection("Radarr").Get<Config>();
            
            SetupDataDirectory();
        }

        public IConfiguration Config { get; }
        
        public Config ConfigRadarr { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<Config>(Config.GetSection("Radarr"));
            services.AddDbContextPool<DatabaseContext>(o => o.UseMySql(ConfigRadarr.Database));

            services.AddSingleton(new GitHubClient(new ProductHeaderValue("RadarrAPI")));
            services.AddHttpClient("AppVeyor")
                .ConfigureHttpClient((serviceProvider, client) =>
                {
                    var config = serviceProvider.GetRequiredService<IOptions<Config>>();

                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", config.Value.AppVeyorApiKey);
                })
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
                {
                    UseCookies = false,
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                });

            services.AddHostedService<QueuedHostedService>();
            services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();

            services.AddTransient<ReleaseService>();
            services.AddTransient<GithubReleaseSource>();
            services.AddTransient<AppVeyorReleaseSource>();
            services.AddTransient<AzureReleaseSource>();

            services.AddSingleton(new TraktClient(Config.GetSection("Trakt")["ClientId"], Config.GetSection("Trakt")["ClientSecret"]));
            services.AddMvc();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory, IApplicationLifetime applicationLifetime)
        {
            UpdateDatabase(app);
            
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseSerilogRequestLogging();
            app.UseMvc();
        }

        private void SetupDataDirectory()
        {
            // Check data path
            if (!Path.IsPathRooted(ConfigRadarr.DataDirectory))
            {
                throw new Exception("DataDirectory path must be absolute.");
            }

            // Create
            Directory.CreateDirectory(ConfigRadarr.DataDirectory);
        }

        private static void UpdateDatabase(IApplicationBuilder app)
        {
            using (var serviceScope = app.ApplicationServices
                   .GetRequiredService<IServiceScopeFactory>()
                   .CreateScope())
            {
                using (var context = serviceScope.ServiceProvider.GetService<DatabaseContext>())
                {
                    context.Database.Migrate();
                }
            }
        }
    }
}
