using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sentry;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace RadarrAPI.Services.BackgroundTasks
{
    public class QueuedHostedService : BackgroundService
    {
        private readonly ILogger _logger;

        private readonly IServiceScopeFactory _serviceScopeFactory;

        private readonly IHub _sentry;

        public QueuedHostedService(
            IBackgroundTaskQueue taskQueue,
            ILoggerFactory loggerFactory,
            IServiceScopeFactory serviceScopeFactory,
            IHub sentry)
        {
            TaskQueue = taskQueue;
            _logger = loggerFactory.CreateLogger<QueuedHostedService>();
            _serviceScopeFactory = serviceScopeFactory;
            _sentry = sentry;
        }

        public IBackgroundTaskQueue TaskQueue { get; }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Queued Hosted Service is starting.");

            while (!cancellationToken.IsCancellationRequested)
            {
                var workItem = await TaskQueue.DequeueAsync(cancellationToken);

                try
                {
                    using (var scope = _serviceScopeFactory.CreateScope())
                    {
                        await workItem(scope.ServiceProvider, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error occurred executing {nameof(workItem)}.");
                    _sentry.CaptureException(ex);
                }
            }

            _logger.LogInformation("Queued Hosted Service is stopping.");
        }
    }
}
