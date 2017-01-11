using Microsoft.AspNetCore.Mvc;
using RadarrAPI.Release;
using RadarrAPI.Update;

namespace RadarrAPI.Controllers
{
    [Route("v1/[controller]")]
    public class WebhookController
    {
        private readonly ReleaseService _releaseService;

        public WebhookController(ReleaseService releaseService)
        {
            _releaseService = releaseService;
        }

        [Route("github")]
        [HttpGet]
        public string GetGithub(Branch branch)
        {
            _releaseService.UpdateReleases(branch);

            return "Thank you.";
        }
    }
}
