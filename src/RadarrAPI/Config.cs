using Newtonsoft.Json;

namespace RadarrAPI
{
    public class Config
    {

        [JsonProperty(Required = Required.Always)]
        public string DataDirectory { get; set; }

    }
}
