using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RadarrAPI.Database;
using RadarrAPI.Database.Models;
using RadarrAPI.Update;
using OperatingSystem = RadarrAPI.Update.OperatingSystem;

namespace RadarrAPI.Controllers
{
    [Route("v1/[controller]")]
    public class DownloadController : Controller
    {
        private readonly DatabaseContext _database;

        public DownloadController(DatabaseContext database)
        {
            _database = database;
        }

        [Route("{branch}/{os}")]
        [HttpGet]
        public ActionResult GetVersionResult(
            [FromRoute(Name = "branch")] Branch updateBranch,
            [FromRoute(Name = "os")] OperatingSystem operatingSystem)
        {
            // Grab the update.
            var update = _database.UpdateEntities
                .Include(x => x.UpdateFiles)
                .Where(x =>
                    x.Branch == updateBranch &&
                    x.UpdateFiles.Any(u => u.OperatingSystem == operatingSystem))
                .OrderByDescending(x => x.ReleaseDate)
                .FirstOrDefault();

            return GetResponse(update, operatingSystem);
        }

        [Route("{branch}/{os}/{version}")]
        [HttpGet]
        public ActionResult GetVersionSpecific(
            [FromRoute(Name = "branch")] Branch updateBranch,
            [FromRoute(Name = "os")] OperatingSystem operatingSystem,
            [FromRoute(Name = "version")] string urlVersion)
        {
            // Grab the update.
            var update = _database.UpdateEntities
                .Include(x => x.UpdateFiles)
                .FirstOrDefault(x => x.Version.Equals(urlVersion) &&
                                     x.Branch == updateBranch &&
                                     x.UpdateFiles.Any(u => u.OperatingSystem == operatingSystem));

            return GetResponse(update, operatingSystem);
        }

        private ActionResult GetResponse(UpdateEntity update, OperatingSystem operatingSystem)
        {
            // Check if the version was found.
            if (update == null)
            {
                return NotFound(new
                {
                    ErrorMessage = "Version not found."
                });
            }

            // Check if update file is present
            var updateFile = update.UpdateFiles.FirstOrDefault(u => u.OperatingSystem == operatingSystem);
            if (updateFile == null)
            {
                return NotFound(new
                {
                    ErrorMessage = "Version download url not found."
                });
            }

            return Redirect(updateFile.Url);
        }
    }
}
