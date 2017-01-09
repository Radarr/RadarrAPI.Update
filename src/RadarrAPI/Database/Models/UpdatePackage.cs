using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace RadarrAPI.Database.Models
{
    public class UpdatePackage
    {

        [JsonIgnore]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Key]
        public int UpdatePackageId { get; set; }

        [JsonConverter(typeof(VersionConverter))]
        [NotMapped]
        public Version Version { get; set; }
        
        [JsonIgnore]
        public string VersionStr
        {
            get { return JsonConvert.SerializeObject(Version, new VersionConverter()); }
            set { Version = JsonConvert.DeserializeObject<Version>(value, new VersionConverter()); }
        }

        [JsonConverter(typeof(IsoDateTimeConverter))]
        public DateTime ReleaseDate { get; set; }

        public string Filename { get; set; }

        public string Url { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public UpdateChanges Changes { get; set; }

        /// <summary>
        ///     Must be a SHA256 hash of the zip file of <see cref="Url"/>.
        /// </summary>
        public string Hash { get; set; }

        public string Branch { get; set; }

    }
}
