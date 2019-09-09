using System.Collections.Generic;
using Newtonsoft.Json;

namespace RadarrAPI.Services.ReleaseCheck.Azure.Responses
{
    public class AzureList<T> where T : class
    {

        [JsonProperty("count")]
        public int Count { get; set; }
        
        [JsonProperty("value")]
        public List<T> Value { get; set; }

    }
}
