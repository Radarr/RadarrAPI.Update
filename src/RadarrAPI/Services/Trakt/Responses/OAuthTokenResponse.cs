namespace RadarrAPI.Services.Trakt.Responses
{
    public class OAuthTokenResponse
    {
        public string AccessToken { get; set; }
        public string TokenType { get; set; }
        public int ExpiresIn { get; set; }
        public string RefreshToken { get; set; }
        public string Scope { get; set; }
        public long CreatedAt { get; set; }
    }
}