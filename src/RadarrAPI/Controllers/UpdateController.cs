using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Octokit;
using RadarrAPI.Extensions;
using RadarrAPI.Update;
using RadarrAPI.Update.Data;
using RadarrAPI.Util;
using Branch = RadarrAPI.Update.Branch;

namespace RadarrAPI.Controllers
{
    [Route("v1/[controller]")]
    public class UpdateController : Controller
    {
        /* TODO: Some improvements this class could use:
         * - Get rid of duplicate code.
         * - Proper caching for faster responses.
         * - Handling downloading of zips somewhere else instead of in a request.
         */

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
                    ErrorMessage = "Invalid version number specified."
                };
            }

            // Check the given operating system.
            if (!Enum.TryParse(urlOs, true, out os))
            {
                return new
                {
                    ErrorMessage = "Invalid operating system specified."
                };
            }

            var releases = await _memoryCache.GetValueAsync("github-radarr", async () => await _gitHubClient.Repository.Release.GetAll("radarr", "radarr"), TimeSpan.FromMinutes(5));
            var validReleases = releases.Where(r =>
                r.TagName.StartsWith("v") &&
                VersionUtil.IsValid(r.TagName.Substring(1)) &&
                r.Prerelease == (branch == Branch.Develop) &&
                r.PublishedAt.HasValue);

            var updatePackages = new List<UpdatePackage>();

            foreach (var release in validReleases)
            {
                // Check if release has been published.
                if (!release.PublishedAt.HasValue) continue;

                // Figure out the zip file for the requested platform.
                ReleaseAsset releaseAsset;
                
                switch (os)
                {
                    case OperatingSystem.Windows:
                        releaseAsset = release.Assets.FirstOrDefault(a => a.Name.ToLower().Contains("windows."));
                        break;
                    case OperatingSystem.Linux:
                        releaseAsset = release.Assets.FirstOrDefault(a => a.Name.ToLower().Contains("linux."));
                        break;
                    case OperatingSystem.Osx:
                        releaseAsset = release.Assets.FirstOrDefault(a => a.Name.ToLower().Contains("osx."));
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                if (releaseAsset == null) continue;

                // Calculate the hash of the zip file.
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

                // Figure out the changes of this release.
                var updateChanges = new UpdateChanges();
                var releaseBody = release.Body;

                var features = RegexUtil.ReleaseFeaturesGroup.Match(releaseBody);
                if (features.Success)
                {
                    foreach (Match match in RegexUtil.ReleaseChange.Matches(features.Groups["features"].Value))
                    {
                        if (match.Success)
                        {
                            updateChanges.New.Add(match.Groups["text"].Value);
                        }
                    }
                }

                var fixes = RegexUtil.ReleaseFixesGroup.Match(releaseBody);
                if (fixes.Success)
                {
                    foreach (Match match in RegexUtil.ReleaseChange.Matches(fixes.Groups["fixes"].Value))
                    {
                        if (match.Success)
                        {
                            updateChanges.Fixed.Add(match.Groups["text"].Value);
                        }
                    }
                }

                // If there were no changes, null updateChanges.
                if (updateChanges.Fixed.Count == 0 && updateChanges.New.Count == 0)
                {
                    updateChanges = null;
                }

                updatePackages.Add(new UpdatePackage
                {
                    Version = new Version(release.TagName.Substring(1)),
                    ReleaseDate = release.PublishedAt.Value.UtcDateTime,
                    Filename = releaseAsset.Name,
                    Url = releaseAsset.BrowserDownloadUrl,
                    Changes = updateChanges,
                    Hash = releaseHash,
                    Branch = branch.ToString().ToLower()
                });   
            }

            // TODO: Check sorting..

            return updatePackages;
        }

        [Route("{branch}")]
        [HttpGet]
        public async Task<object> GetUpdates([FromRoute(Name = "branch")]string updateBranch, [FromQuery(Name = "version")]string urlVersion, [FromQuery(Name = "os")]string urlOs)
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
                    ErrorMessage = "Invalid version number specified."
                };
            }

            // Check the given operating system.
            if (!Enum.TryParse(urlOs, true, out os))
            {
                return new
                {
                    ErrorMessage = "Invalid operating system specified."
                };
            }

            var releases = await _gitHubClient.Repository.Release.GetAll("radarr", "radarr");
            var release = releases.FirstOrDefault(r =>
                r.TagName.StartsWith("v") && 
                VersionUtil.IsValid(r.TagName.Substring(1)) &&
                r.Prerelease == (branch == Branch.Develop) &&
                r.PublishedAt.HasValue);

            if (release == null)
            {
                return new
                {
                    ErrorMessage = "No release was found."
                };
            }

            // Check if requested version needs this update.
            var releaseVersion = new Version(release.TagName.Substring(1));
            if (releaseVersion.CompareTo(version) <= 0)
            {
                return new UpdatePackageContainer
                {
                    Available = false
                };
            }

            // Figure out the zip file for the requested platform.
            ReleaseAsset releaseAsset;

            switch (os)
            {
                case OperatingSystem.Windows:
                    releaseAsset = release.Assets.FirstOrDefault(a => a.Name.ToLower().Contains("windows."));
                    break;
                case OperatingSystem.Linux:
                    releaseAsset = release.Assets.FirstOrDefault(a => a.Name.ToLower().Contains("linux."));
                    break;
                case OperatingSystem.Osx:
                    releaseAsset = release.Assets.FirstOrDefault(a => a.Name.ToLower().Contains("osx."));
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (releaseAsset == null)
            {
                return new
                {
                    ErrorMessage = $"Release for platform {os} was not found."
                };
            }

            // Calculate the hash of the zip file.
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

            // Figure out the changes of this release.
            var updateChanges = new UpdateChanges();
            var releaseBody = release.Body;

            var features = RegexUtil.ReleaseFeaturesGroup.Match(releaseBody);
            if (features.Success)
            {
                foreach (Match match in RegexUtil.ReleaseChange.Matches(features.Groups["features"].Value))
                {
                    if (match.Success)
                    {
                        updateChanges.New.Add(match.Groups["text"].Value);
                    }
                }
            }

            var fixes = RegexUtil.ReleaseFixesGroup.Match(releaseBody);
            if (fixes.Success)
            {
                foreach (Match match in RegexUtil.ReleaseChange.Matches(fixes.Groups["fixes"].Value))
                {
                    if (match.Success)
                    {
                        updateChanges.Fixed.Add(match.Groups["text"].Value);
                    }
                }
            }

            // If there were no changes, null updateChanges.
            if (updateChanges.Fixed.Count == 0 && updateChanges.New.Count == 0)
            {
                updateChanges = null;
            }

            return new UpdatePackageContainer
            {
                Available = true,
                UpdatePackage = new UpdatePackage
                {
                    Version = releaseVersion,
                    ReleaseDate = release.PublishedAt.Value.UtcDateTime,
                    Filename = releaseAsset.Name,
                    Url = releaseAsset.BrowserDownloadUrl,
                    Changes = updateChanges,
                    Hash = releaseHash,
                    Branch = branch.ToString().ToLower()
                }
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
