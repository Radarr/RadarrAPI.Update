using System;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using RadarrAPI.Update;

namespace RadarrAPI.Release
{
    public abstract class ReleaseSourceBase
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly Mutex _fetchMutex;

        protected ReleaseSourceBase(Branch branch)
        {
            Branch = branch;

            _fetchMutex = new Mutex();
        }

        public Branch Branch { get; }
        
        public async Task StartFetchReleasesAsync()
        {
            try
            {
                _fetchMutex.WaitOne();

                // Do stuff
            }
            finally
            {
                _fetchMutex.ReleaseMutex();
            }
        }
    }
}