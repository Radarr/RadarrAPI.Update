using Newtonsoft.Json;

namespace RadarrAPI.Services.ReleaseCheck.AppVeyor.Responses
{
    public class AppVeyorProjectLastBuild
    {
        
        [JsonProperty("build")]
        public AppVeyorProjectBuild Build { get; set; }

    }
}
