using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NLog;
using RadarrAPI.Database;
using RadarrAPI.Release.Teamcity.Responses;
using RadarrAPI.Update;
using Microsoft.EntityFrameworkCore;
using RadarrAPI.Database.Models;
using System.Globalization;

namespace RadarrAPI.Release.Teamcity
{
    public class TeamcityReleaseSource : ReleaseSourceBase
    {
        private const string AccountName = "galli-leo";
        private const string ProjectSlug = "radarr-usby1";
        private const string ServerURL = "https://builds.radarr.video";

        private readonly HttpClient _httpClient;

        private readonly HttpClient _downloadHttpClient;

        private int? _lastBuildId;

        public TeamcityReleaseSource(IServiceProvider serviceProvider, Branch branch) : base(serviceProvider, branch)
        {
            var config = serviceProvider.GetService<IOptions<Config>>().Value;

            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{config.TeamcityUser}:{config.TeamcityPass}")));

            _downloadHttpClient = new HttpClient();
            _downloadHttpClient.DefaultRequestHeaders.Authorization = _httpClient.DefaultRequestHeaders.Authorization;
        }

        protected override async Task DoFetchReleasesAsync()
        {
            //https://builds.radarr.video/httpAuth/app/rest/builds/?locator=buildType:Radarr_Build,count:10&fields=build(id,number,status,branchName,defaultBranch,state,artifacts(file))
            var buildUrl = $"{ServerURL}/httpAuth/app/rest/builds/?locator=buildType:Radarr_Build,count:10&fields=build(id,number,status,finishDate,branchName,defaultBranch,state,artifacts(file),changes(count,change(comment)))";

            var buildData = await _httpClient.GetStringAsync(buildUrl);
            var builds = JsonConvert.DeserializeObject<TeamcityResponse>(buildData);

            // Store here temporarily so we don't break on not processed builds.
            var lastBuild = _lastBuildId;
            var database = ServiceProvider.GetService<DatabaseContext>();

            foreach (var build in builds.Build)
            {
                if (lastBuild.HasValue &&
                    lastBuild.Value >= build.Id) break;

                // Make sure we dont distribute;
                // - pull requests,
                // - unsuccesful builds,
                // - tagged builds (duplicate).
                if (!build.DefaultBranch
                   || build.Status != "SUCCESS"
                   || build.State != "finished"
                   || build.Changes.Count == 0) continue;

                // Get an updateEntity
                var updateEntity = database.UpdateEntities
                    .Include(x => x.UpdateFiles)
                    .FirstOrDefault(x => x.Version.Equals(build.Number) && x.Branch.Equals(ReleaseBranch));

                if (updateEntity == null)
                {
                    // Create update object
                    List<string> changeList = new List<string>(string.Join("\n", build.Changes.Change.Select(c => string.Join("\n", c.Comment.Split('\n').Where(t => !string.IsNullOrEmpty(t))))).Split('\n'));
                    List<string> fixes = changeList.Where(c => c.ToLower().Contains("fix")).ToList();
                    List<string> newStuff = changeList.Where(c => !c.ToLower().Contains("fix")).ToList();

                    updateEntity = new UpdateEntity
                    {
                        Version = build.Number,
                        ReleaseDate = DateTime.ParseExact(build.FinishDate, "yyyyMMddTHHmmss+0000", CultureInfo.InvariantCulture),
                        Branch = ReleaseBranch,
                        New = newStuff,
                        Fixed = fixes
                    };

                    // Start tracking this object
                    await database.AddAsync(updateEntity);
                }

                // Process artifacts
                foreach (var artifact in build.Artifacts.File)
                {
                    // Detect target operating system.
                    OperatingSystem operatingSystem;

                    if (artifact.Name.Contains("windows."))
                    {
                        operatingSystem = OperatingSystem.Windows;
                    }
                    else if (artifact.Name.Contains("linux."))
                    {
                        operatingSystem = OperatingSystem.Linux;
                    }
                    else if (artifact.Name.Contains("osx."))
                    {
                        operatingSystem = OperatingSystem.Osx;
                    }
                    else
                    {
                        continue;
                    }

                    // Check if exists in database.
                    var updateFileEntity = database.UpdateFileEntities
                        .FirstOrDefault(x =>
                            x.UpdateEntityId == updateEntity.UpdateEntityId &&
                            x.OperatingSystem == operatingSystem);

                    if (updateFileEntity != null) continue;

                    // Calculate the hash of the zip file.
                    var releaseDownloadUrl = $"{ServerURL}{artifact.Href.Replace("metadata", "content")}";
                    var releaseFileName = artifact.Name;
                    var releaseZip = Path.Combine(Config.DataDirectory, ReleaseBranch.ToString(), releaseFileName);
                    string releaseHash;

                    if (!System.IO.File.Exists(releaseZip))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(releaseZip));
                        System.IO.File.WriteAllBytes(releaseZip, await _downloadHttpClient.GetByteArrayAsync(releaseDownloadUrl));
                    }

                    using (var stream = System.IO.File.OpenRead(releaseZip))
                    {
                        using (var sha = SHA256.Create())
                        {
                            releaseHash = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLower();
                        }
                    }

                    //File.Delete(releaseZip);

                    // Add to database.
                    updateEntity.UpdateFiles.Add(new UpdateFileEntity
                    {
                        OperatingSystem = operatingSystem,
                        Filename = releaseFileName,
                        Url = releaseDownloadUrl,
                        Hash = releaseHash
                    });
                }

                // Save all changes to the database.
                await database.SaveChangesAsync();

                // Make sure we atleast skip this build next time.
                if (_lastBuildId == null ||
                    _lastBuildId.Value < build.Id)
                {
                    _lastBuildId = build.Id;
                }
            }
        }
    }
}
