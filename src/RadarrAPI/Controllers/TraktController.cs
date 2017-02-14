using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RadarrAPI.Database;
using RadarrAPI.Update;
using RadarrAPI.Update.Data;
using System.Threading.Tasks;
using Newtonsoft.Json;


namespace RadarrAPI.Controllers
{
    [Route("authorize")]
    public class TraktController : Controller
    {
        //private readonly DatabaseContext _database;

        public TraktController() //DatabaseContext database)
        {
            //_database = database;
        }

        public class Rootobject
        {
            public string access_token { get; set; }
            public string token_type { get; set; }
            public int expires_in { get; set; }
            public string refresh_token { get; set; }
            public string scope { get; set; }
        }

        [Route("trakt_refresh")]
        [HttpGet, HttpPost]
        public async Task<string> Refresh([FromQuery(Name = "token")] string token)
        {
            if (token !=null)
            {
                //var client_id = "657bb899dcb81ec8ee838ff09f6e013ff7c740bf0ccfa54dd41e791b9a70b2f0"; //radarr
                var client_id = "e5e89432438f72a25a09313b0724f6752476a4f1595bca7c16e9819628eaf022"; //radarr_test
                var client_secret = "b6ebfc6499fd1a15e8f27d1cace8e603e9fa73fae48a5ad0d1fb605d8a22af51"; //radarr_test TODO: load from outside sourceTree so it doesn't end up in github
                var redirect_url = "http://" + this.HttpContext.Request.Host + "/authorize/trakt";
                string postData = "{\"refresh_token\":\"" + token + "\"";
                postData += ",\"client_id\":\"" + client_id + "\"";
                postData += ",\"client_secret\":\"" + client_secret + "\"";
                postData += ",\"redirect_uri\":\"" + redirect_url + "\"";
                postData += ",\"grant_type\":\"refresh_token\"}";
                byte[] byteArray = Encoding.UTF8.GetBytes(postData);

                HttpWebRequest webRequest = (HttpWebRequest)HttpWebRequest.CreateHttp("https://api.trakt.tv/oauth/token");
                webRequest.Method = "POST";
                webRequest.ContentType = "application/json";

                var reqStream = await webRequest.GetRequestStreamAsync();
                reqStream.Write(byteArray, 0, byteArray.Length);

                var response = await webRequest.GetResponseAsync();
                Stream resStream = response.GetResponseStream();

                var reader = new StreamReader(resStream);
                var responseString = await reader.ReadToEndAsync();

                return responseString;
            }
            return "Invalid Request";
            
        }

        [Route("trakt")]
        [HttpGet]
        public async Task<object> GetChanges([FromQuery(Name = "target")]string target, [FromQuery(Name = "code")]string code, [FromQuery(Name = "state")] string state)
        {
            //var client_id = "657bb899dcb81ec8ee838ff09f6e013ff7c740bf0ccfa54dd41e791b9a70b2f0"; //radarr
            var client_id = "e5e89432438f72a25a09313b0724f6752476a4f1595bca7c16e9819628eaf022"; //radarr_test
            var client_secret = "b6ebfc6499fd1a15e8f27d1cace8e603e9fa73fae48a5ad0d1fb605d8a22af51"; //TODO: load from outside sourceTree on github (so its kept secret)
            var redirect_url = "http://" + this.HttpContext.Request.Host + "/authorize/trakt";
            if (target != null)
            {
                //need to generate unique, unguessable state value
                //store this state value along with target since state will be used to look up target later
                //these state values should expire after sometime.... maybe?
                //for now I am just passing the target in the state parameter
                //but this will allow someone to use radarrAPI the same way I originally used
                //api.couchpota.to
                //storing the target and using an unguessable state makes sure
                //that radarrAPI only responds to requests originating from radarrAPI (and radarr) and not
                //some other app
                var newState = target;
                return Redirect("https://trakt.tv/oauth/authorize?response_type=code&client_id=" + client_id + "&redirect_uri=" + redirect_url + (state != "" ? "&state=" + newState : ""));
            }
            else if (code !=null && state != null)
            {
                //check to make sure the state parameter is valid
                //use state parameter to look up target;
                target = state;
                string postData = "{\"code\":\""+ code+"\"";
                postData += ",\"client_id\":\""+client_id+"\"";
                postData += ",\"client_secret\":\"" + client_secret + "\"";
                postData += ",\"redirect_uri\":\""+redirect_url+"\"";
                postData += ",\"grant_type\":\"authorization_code\"}";
                byte[] byteArray = Encoding.UTF8.GetBytes(postData);
                
                HttpWebRequest webRequest = (HttpWebRequest)HttpWebRequest.CreateHttp("https://api.trakt.tv/oauth/token");
                webRequest.Method = "POST";
                webRequest.ContentType = "application/json";

                var reqStream = await webRequest.GetRequestStreamAsync();
                reqStream.Write(byteArray, 0, byteArray.Length);
                
                var response = await webRequest.GetResponseAsync();
                Stream resStream = response.GetResponseStream();

                var reader = new StreamReader(resStream);
                var responseString = await reader.ReadToEndAsync();
                Rootobject j1 = Newtonsoft.Json.JsonConvert.DeserializeObject<Rootobject>(responseString);
                return Redirect(target + "?oauth=" + j1.access_token + "&refresh=" + j1.refresh_token);               
            }

            return Redirect("Error");

        }
    }
}
