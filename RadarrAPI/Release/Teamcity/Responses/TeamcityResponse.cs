using System;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
namespace RadarrAPI.Release.Teamcity.Responses
{
    public class File
    {

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("href")]
        public string Href { get; set; }
    }

    public class Change
    {

        [JsonProperty("comment")]
        public string Comment { get; set; }
    }

    public class Changes
    {

        [JsonProperty("change")]
        public IList<Change> Change { get; set; }

        [JsonProperty("count")]
        public int Count { get; set; }
    }

    public class Artifacts
    {

        [JsonProperty("file")]
        public IList<File> File { get; set; }
    }

    public class Build
    {

        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("number")]
        public string Number { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("state")]
        public string State { get; set; }

        [JsonProperty("branchName")]
        public string BranchName { get; set; }

        [JsonProperty("defaultBranch")]
        public bool DefaultBranch { get; set; }

        [JsonProperty("finishDate")]
        public string FinishDate { get; set; }

        [JsonProperty("changes")]
        public Changes Changes { get; set; }

        [JsonProperty("artifacts")]
        public Artifacts Artifacts { get; set; }
    }

    public class TeamcityResponse
    {

        [JsonProperty("build")]
        public IList<Build> Build { get; set; }
    }

}
