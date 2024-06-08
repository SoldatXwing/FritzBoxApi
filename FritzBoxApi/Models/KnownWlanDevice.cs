using Newtonsoft.Json;

public class KnownWlanDevice
{
    [JsonProperty("uid")]
    public string Uid { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("delete")]
    public Delete Delete { get; set; }

    [JsonProperty("type")]
    public string Type { get; set; }

    [JsonProperty("bands")]
    public Dictionary<string, Band> Bands { get; set; }

    [JsonProperty("ip")]
    public string Ip { get; set; }
}
public class Delete
{
    [JsonProperty("deleteable")]
    public bool Deleteable { get; set; }

    [JsonProperty("reason")]
    public string Reason { get; set; }
}
public class Band
{
    [JsonProperty("mac")]
    public string Mac { get; set; }

    [JsonProperty("cipher")]
    public string Cipher { get; set; }

    [JsonProperty("rssi")]
    public int Rssi { get; set; }

    [JsonProperty("rate")]
    public Rate Rate { get; set; }

    [JsonProperty("props")]
    public string Props { get; set; }
}
public class Rate
{
    [JsonProperty("us")]
    public string Us { get; set; }

    [JsonProperty("ds")]
    public string Ds { get; set; }
}
