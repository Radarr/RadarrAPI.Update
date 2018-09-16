using Newtonsoft.Json;

namespace RadarrAPI.Services.ReleaseCheck.AppVeyor.Responses
{
    public class AppVeyorArtifact
    {

        [JsonProperty("fileName", Required = Required.Always)]
        public string FileName;

        [JsonProperty("type", Required = Required.Always)]
        public string Type;

        [JsonProperty("size", Required = Required.Always)]
        public int Size;

    }
}
