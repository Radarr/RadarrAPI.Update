using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Branch = RadarrAPI.Update.Branch;

namespace RadarrAPI.Controllers
{
    [Route("v1/[controller]")]
    public class UpdateController : Controller
    {
        [Route("{branch}/changes")]
        [HttpGet]
        public async Task<object> GetChanges([FromRoute(Name = "branch")]Branch updateBranch, [FromQuery(Name = "version")]string urlVersion, [FromQuery(Name = "os")]string urlOs)
        {
            return new
            {
                ErrorMessage = "Hello.",
                B = updateBranch
            };
        }

        [Route("{branch}")]
        [HttpGet]
        public async Task<object> GetUpdates([FromRoute(Name = "branch")]Branch updateBranch, [FromQuery(Name = "version")]string urlVersion, [FromQuery(Name = "os")]string urlOs)
        {
            return new
            {
                ErrorMessage = "World.",
                B = updateBranch
            };
        }
    }
}
