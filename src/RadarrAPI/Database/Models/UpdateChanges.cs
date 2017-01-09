using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace RadarrAPI.Database.Models
{
    public class UpdateChanges
    {

        [JsonIgnore]
        [ForeignKey("UpdatePackage")]
        public int UpdateChangesId { get; set; }
        
        [JsonIgnore]
        public virtual UpdatePackage UpdatePackage { get; set; }

        [NotMapped]
        public List<string> New { get; set; } = new List<string>();

        [NotMapped]
        public List<string> Fixed { get; set; } = new List<string>();

        [JsonIgnore]
        public string NewStr
        {
            get { return JsonConvert.SerializeObject(New); }
            set { New = JsonConvert.DeserializeObject<List<string>>(value); }
        }

        [JsonIgnore]
        public string FixedStr
        {
            get { return JsonConvert.SerializeObject(Fixed); }
            set { Fixed = JsonConvert.DeserializeObject<List<string>>(value); }
        }

    }
}
