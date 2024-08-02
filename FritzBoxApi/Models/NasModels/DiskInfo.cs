using Newtonsoft.Json;
using System.Reflection.Metadata;

namespace FritzBoxApi.Models.NasModels
{
    public class DiskInfo
    {
        [JsonProperty("used")]
        private double Used { get; set; }
        [JsonProperty("total")]
        private double Total { get; set; }
        [JsonProperty("free")]
        private double Free { get; set; }
        public double TotalInMb => Total / 1048576.0;
        public double FreeInMb => Free / 1048576.0;
        public double UsedInMb => Used / 1048576.0;
    }
}