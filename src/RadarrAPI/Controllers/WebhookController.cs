using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RadarrAPI.Services.BackgroundTasks;
using RadarrAPI.Services.ReleaseCheck;
using RadarrAPI.Update;

namespace RadarrAPI.Controllers
{
    [Route("v1/[controller]")]
    public class WebhookController
    {
        private readonly IServiceProvider _services;
        
        private readonly IBackgroundTaskQueue _queue;

        private readonly Config _config;

        public WebhookController(IServiceProvider services, IBackgroundTaskQueue queue, IOptions<Config> optionsConfig)
        {
            _services = services;
            _queue = queue;
            _config = optionsConfig.Value;
        }

        [Route("refresh")]
        [HttpGet, HttpPost]
        public string Refresh([FromQuery] Branch branch, [FromQuery(Name = "api_key")] string apiKey)
        {
            if (!_config.ApiKey.Equals(apiKey))
            {
                return "No, thank you.";
            }

            _queue.QueueBackgroundWorkItem(async token =>
            {
                using (var scope = _services.CreateScope())
                {
                    var releaseService = scope.ServiceProvider.GetRequiredService<ReleaseService>();
                    await releaseService.UpdateReleasesAsync(branch);
                }
            });

            return "Thank you.";
        }
    }
}
