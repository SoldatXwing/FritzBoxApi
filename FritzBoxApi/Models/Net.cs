using Newtonsoft.Json;

public class Net
{
    [JsonProperty("devices")]
    public List<Device> Devices { get; set; }
}
