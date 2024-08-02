using Newtonsoft.Json;

namespace FritzBoxApi.Models.NasModels
{
    public class FirtzBoxNasResponse
    {
        [JsonProperty("diskInfo")]
        public DiskInfo DiskInfo { get; set; }
        [JsonProperty("files")]
        public List<NasFile> Files { get; set; }
        [JsonProperty("directories")]
        public List<NasDirectory> Directories { get; set; }
        [JsonProperty("writeright")]
        public bool WriteRight { get; set; }
        [JsonProperty("browse")]
        public Browse Browse { get; set; }
    }
}
