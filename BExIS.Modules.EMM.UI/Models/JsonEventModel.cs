using Newtonsoft.Json;
using System.Collections.Generic;
using Vaiona.Utils.Cfg;
using static System.Collections.Specialized.BitVector32;

namespace BExIS.Modules.EMM.UI.Models
{
    public class JsonEventModel
    {

        [JsonProperty("registration")]
        public List<Registration> Registration { get; set; }

        public JsonEventModel()
        {
            Registration = new List<Registration>();
        } 
    }

    public class Registration
    {
        public Registration()
        {
            Entries = new List<Entry>();
        }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("entries")]
        public List<Entry> Entries { get; set; } 
    }


    public class Entry
    {
        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("options")]
        public List<string> Options { get; set; }
    }

}