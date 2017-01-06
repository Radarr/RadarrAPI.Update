using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Octokit;
using RadarrAPI.Update;
using RadarrAPI.Update.Data;
using RadarrAPI.Util;
using Branch = RadarrAPI.Update.Branch;

namespace RadarrAPI.Controllers
{
    [Route("v1/[controller]")]
    public class UpdateController : Controller
    {
        private readonly GitHubClient _gitHubClient;
        private readonly IMemoryCache _memoryCache;
        private readonly Config _config;
        private readonly HttpClient _httpClient;

        public UpdateController(GitHubClient gitHubClient, IMemoryCache memoryCache, IOptions<Config> config)
        {
            _gitHubClient = gitHubClient;
            _memoryCache = memoryCache;
            _config = config.Value;
            _httpClient = new HttpClient();
        }

        [Route("{branch}/changes")]
        [HttpGet]
        public async Task<object> GetChanges([FromRoute(Name = "branch")]string updateBranch, [FromQuery(Name = "version")]string urlVersion, [FromQuery(Name = "os")]string urlOs)
        {
            Branch branch;
            Version version;
            OperatingSystem os;

            // Check the update branch, default to master.
            if (!Enum.TryParse(updateBranch, true, out branch))
            {
                branch = Branch.Master;
            }

            // Check the given version.
            if (!Version.TryParse(urlVersion, out version))
            {
                return new
                {
                    Message = "Invalid version number specified."
                };
            }

            // Check the given operating system.
            if (!Enum.TryParse(urlOs, true, out os))
            {
                return new
                {
                    Message = "Invalid operating system specified."
                };
            }

            // return await _memoryCache.GetValueAsync("asd", async () => await _gitHubClient.Repository.Release.GetAll("galli-leo", "radarr"), TimeSpan.FromMinutes(5));

            var releases = await _gitHubClient.Repository.Release.GetAll("galli-leo", "radarr");
            var validReleases = releases.Where(r => r.TagName.StartsWith("v") && VersionUtil.IsValid(r.TagName.Substring(1)) && r.Prerelease == (branch == Branch.Develop));

            var updatePackages = new List<UpdatePackage>();

            foreach (var release in validReleases)
            {
                if (!release.PublishedAt.HasValue) continue;

                ReleaseAsset releaseAsset;

                switch (os)
                {
                    case OperatingSystem.Windows:
                        releaseAsset = release.Assets.FirstOrDefault(a => a.Name.StartsWith("Radarr_Windows"));
                        break;
                    case OperatingSystem.Linux:
                        releaseAsset = release.Assets.FirstOrDefault(a => a.Name.StartsWith("Radarr_Mono"));
                        break;
                    case OperatingSystem.Osx:
                        releaseAsset = release.Assets.FirstOrDefault(a => a.Name.StartsWith("Radarr_OSX"));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                if (releaseAsset == null) continue;

                var releaseZip = Path.Combine(_config.DataDirectory, releaseAsset.Name);
                string releaseHash;

                if (!System.IO.File.Exists(releaseZip))
                {
                    System.IO.File.WriteAllBytes(releaseZip, await _httpClient.GetByteArrayAsync(releaseAsset.BrowserDownloadUrl));
                }

                using (var stream = System.IO.File.OpenRead(releaseZip))
                {
                    using (var sha = SHA256.Create())
                    {
                        releaseHash = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLower();
                    }
                }

                updatePackages.Add(new UpdatePackage
                {
                    Version = new Version(release.TagName.Substring(1)),
                    ReleaseDate = release.PublishedAt.Value.UtcDateTime,
                    Filename = releaseAsset.Name,
                    Url = releaseAsset.BrowserDownloadUrl,
                    Changes = new UpdateChanges
                    {
                        New = new List<string>(),
                        Fixed = new List<string>()
                    },
                    Hash = releaseHash,
                    Branch = branch.ToString().ToLower()
                });   
            }

            return updatePackages;
        }

        [Route("{branch}")]
        [HttpGet]
        public object GetUpdates([FromRoute(Name = "branch")]string updateBranch, [FromQuery(Name = "version")]string urlVersion, [FromQuery(Name = "os")]string urlOs)
        {
            Branch branch;
            Version version;
            OperatingSystem os;

            // Check the update branch, default to master.
            if (!Enum.TryParse(updateBranch, true, out branch))
            {
                branch = Branch.Master;
            }

            // Check the given version.
            if (!Version.TryParse(urlVersion, out version))
            {
                return new
                {
                    Message = "Invalid version number specified."
                };
            }
            
            // Check the given operating system.
            if (!Enum.TryParse(urlOs, true, out os))
            {
                return new
                {
                    Message = "Invalid operating system specified."
                };
            }

            // Continue with the request
            return new
            {
                Branch = branch.ToString(),
                Version = version.ToString(),
                Os = os.ToString()
            };
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _httpClient.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
