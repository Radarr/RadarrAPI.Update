using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RadarrAPI.Options;
using RadarrAPI.Services.Trakt.Responses;

namespace RadarrAPI.Services.Trakt
{
    public class TraktService
    {
        private const string BaseUrl = "https://api.trakt.tv";
        
        private readonly TraktOptions _options;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;
        
        public TraktService(IOptions<TraktOptions> traktOptions, IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
        {
            _options = traktOptions.Value;
            _httpClientFactory = httpClientFactory;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<string> CreateAuthorizationUrlAsync(string state)
        {
            var uriParams = new Dictionary<string, string>
            {
                ["response_type"] = "code",
                ["client_id"] = _options.ClientId,
                ["redirect_uri"] = GetRedirectUri(),
                ["state"] = state
            };
            
            var encodedUriContent = new FormUrlEncodedContent(uriParams);
            var encodedUri = await encodedUriContent.ReadAsStringAsync();

            return $"{BaseUrl}/oauth/authorize?{encodedUri}"; 
        }

        public async Task<OAuthTokenResponse> GetAuthorizationAsync(string code)
        {
            var client = _httpClientFactory.CreateClient(nameof(TraktService));
            var url = $"{BaseUrl}/oauth/token";
            var content = new Dictionary<string, string>
            {
                ["code"] = code,
                ["client_id"] = _options.ClientId,
                ["client_secret"] = _options.ClientSecret,
                ["redirect_uri"] = GetRedirectUri(),
                ["grant_type"] = "authorization_code"
            };

            using (var response = await client.PostAsJsonAsync(url, content))
            {
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return JsonConvert.DeserializeObject<OAuthTokenResponse>(await response.Content.ReadAsStringAsync());
                }
            }
            
            return null;
        }

        public async Task<OAuthTokenResponse> RefreshAuthorizationAsync(string refreshToken)
        {
            var client = _httpClientFactory.CreateClient(nameof(TraktService));
            var url = $"{BaseUrl}/oauth/token";
            var content = new Dictionary<string, string>
            {
                ["refresh_token"] = refreshToken,
                ["client_id"] = _options.ClientId,
                ["client_secret"] = _options.ClientSecret,
                ["redirect_uri"] = GetRedirectUri(),
                ["grant_type"] = "refresh_token"
            };
            
            using (var response = await client.PostAsJsonAsync(url, content))
            {
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return JsonConvert.DeserializeObject<OAuthTokenResponse>(await response.Content.ReadAsStringAsync());
                }
            }
            
            return null;
        }
        
        private string GetRedirectUri()
        {
            var context = _httpContextAccessor.HttpContext;
            var redirectUri = new StringBuilder();
            
            redirectUri.Append(context.Request.IsHttps ? "https://" : "http://");
            redirectUri.Append(context.Request.Host);
            redirectUri.Append("/v1/trakt/callback");

            return redirectUri.ToString();
        }
    }
}