using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using NLog;
using RadarrAPI.Release.Github;
using RadarrAPI.Update;

namespace RadarrAPI.Release
{
    public class ReleaseService
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly ConcurrentDictionary<Branch, ReleaseSourceBase> _releaseBranches;

        public ReleaseService()
        {
            _releaseBranches = new ConcurrentDictionary<Branch, ReleaseSourceBase>();
            _releaseBranches.TryAdd(Branch.Develop, new GithubReleaseSource(Branch.Develop));
        }

        public void UpdateReleases(Branch branch)
        {
            ReleaseSourceBase releaseSourceBase;

            if (!_releaseBranches.TryGetValue(branch, out releaseSourceBase))
            {
                throw new NotImplementedException($"{branch} does not have a release source.");
            }

            Logger.Warn("-- Task started");

            Task.Factory.StartNew(async () =>
            {
                await releaseSourceBase.StartFetchReleasesAsync();
            });

            Logger.Warn("-- Task ended");
        }
    }
}
