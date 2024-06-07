using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FritzBoxApi.Models
{
    public class FritzBoxResponse
    {
        [JsonProperty("data")]
        public Data Data { get; set; }
    }
}
