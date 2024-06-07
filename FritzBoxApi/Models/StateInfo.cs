using Newtonsoft.Json;

public class StateInfo
{
    [JsonProperty("nexustrust")]
    public bool NexusTrust { get; set; }

    [JsonProperty("active")]
    public bool Active { get; set; }

    [JsonProperty("online")]
    public bool? Online { get; set; }
}
