using Newtonsoft.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;

public class Net
{
    [JsonProperty("devices")]
    public List<Device> Devices { get; set; }
}

