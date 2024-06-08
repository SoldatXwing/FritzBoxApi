using Newtonsoft.Json;

public class Device
{
    [JsonProperty("own_client_device")]
    public bool OwnClientDevice { get; set; }

    [JsonProperty("type")]
    public string Type { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("stateinfo")]
    public StateInfo StateInfo { get; set; }

    [JsonProperty("url")]
    public string Url { get; set; }

    [JsonProperty("desc")]
    public string Description { get; set; }
    public string Ip {  get; set; }
}
