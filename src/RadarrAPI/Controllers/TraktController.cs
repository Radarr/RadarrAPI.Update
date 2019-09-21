using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RadarrAPI.Database;
using RadarrAPI.Database.Models;
using RadarrAPI.Services.Trakt;

namespace RadarrAPI.Controllers
{
    [Route("v1/[controller]")]
    public class TraktController : Controller
    {
        private readonly DatabaseContext _database;
        private readonly TraktService _traktService;

        public TraktController(DatabaseContext database, TraktService traktService)
        {
            _database = database;
            _traktService = traktService;
        }

        [Route("redirect")]
        [HttpGet]
        public async Task<IActionResult> RedirectToTrakt([FromQuery(Name = "target")] string target)
        {
            var validTarget = Uri.TryCreate(target, UriKind.Absolute, out var uriResult) && (uriResult.Scheme == "http" || uriResult.Scheme == "https");
            if (!validTarget)
            {
                return BadRequest("Invalid target specified.");
            }

            var traktEntity = new TraktEntity
            {
                State = Guid.NewGuid(),
                Target = target,
                CreatedAt = DateTime.UtcNow
            };

            await _database.AddAsync(traktEntity);
            await _database.SaveChangesAsync();
            
            return Redirect(await _traktService.CreateAuthorizationUrlAsync(traktEntity.State.ToString()));
        }

        [Route("callback")]
        [HttpGet]
        public async Task<IActionResult> TraktCallback([FromQuery(Name = "code")] string code, [FromQuery(Name = "state")] string stateStr)
        {
            if (!Guid.TryParse(stateStr, out var state))
            {
                return BadRequest("Invalid state specified.");
            }

            var traktEntity = await _database.TraktEntities.FirstOrDefaultAsync(x => x.State.Equals(state));
            if (traktEntity == null)
            {
                return BadRequest("Unknown state specified.");
            }

            _database.Remove(traktEntity);
            await _database.SaveChangesAsync();

            var traktAuth = await _traktService.GetAuthorizationAsync(code);
            if (traktAuth == null)
            {
                return BadRequest("Received trakt token was invalid.");
            }

            return Redirect($"{traktEntity.Target}?access={traktAuth.AccessToken}&refresh={traktAuth.RefreshToken}");
        }

        [Route("refresh")]
        [HttpGet]
        public async Task<IActionResult> TraktCallback([FromQuery(Name = "refresh")] string refresh)
        {
            if (string.IsNullOrWhiteSpace(refresh))
            {
                return BadRequest("Invalid refresh code specified.");
            }
            
            var traktAuth = await _traktService.RefreshAuthorizationAsync(refresh);
            if (traktAuth == null)
            {
                return BadRequest("Received trakt token was invalid.");
            }
                
            return Ok(traktAuth);
            
//            try
//            {
//            }
//            catch (TraktAuthenticationException e)
//            {
//                return StatusCode(401, new
//                {
//                    Message = "Invalid refresh token specified.",
//                    MessageTrakt = e.Message.Replace("\n", ", ")
//                });
//            }
        }
    }
}
