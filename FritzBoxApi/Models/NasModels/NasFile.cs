using Newtonsoft.Json;

namespace FritzBoxApi.Models.NasModels
{
    public class NasFile
    {
        [JsonProperty("path")]
        public string Path { get; set; }
        [JsonProperty("shared")]
        public bool Shared { get; set; }
        [JsonProperty("storagetype")]
        public string StorageType { get; set; }
        [JsonProperty("type")]
        public string Type { get; set; }
        [JsonProperty("timestamp")]
        public long UnixTimeStamp { get; set; }
    }
}
