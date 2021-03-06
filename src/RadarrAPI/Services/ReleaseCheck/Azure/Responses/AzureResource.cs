﻿using Newtonsoft.Json;

namespace RadarrAPI.Services.ReleaseCheck.Azure.Responses
{
    public class AzureResource
    {

        [JsonProperty("type", Required = Required.Always)]
        public string Type { get; set; }

        [JsonProperty("data", Required = Required.Always)]
        public string Data { get; set; }

    }
}
