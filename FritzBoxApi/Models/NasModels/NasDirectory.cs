using Newtonsoft.Json;

namespace FritzBoxApi.Models.NasModels
{
    public class NasDirectory
    {
        [JsonProperty("path")]
        public string Path { get; set; }

        [JsonProperty("shared")]
        public bool Shared { get; set; }

        [JsonProperty("storageType")]
        public string StorageType { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("timestamp")]
        public int Timestamp { get; set; }

        [JsonProperty("filename")]
        public string Filename { get; set; }
    }
}