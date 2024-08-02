using Newtonsoft.Json;

namespace FritzBoxApi.Models.NasModels
{
    public class Browse
    {
        [JsonProperty("path")]
        public string Path { get; set; }

        [JsonProperty("index")]
        public int Index { get; set; }

        [JsonProperty("totalCount")]
        public int TotalCount { get; set; }

        [JsonProperty("finished")]
        public bool Finished { get; set; }

        [JsonProperty("mode")]
        public string Mode { get; set; }

        [JsonProperty("limit")]
        public int Limit { get; set; }

        [JsonProperty("sorting")]
        public string Sorting { get; set; }
    }
}