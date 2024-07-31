using Newtonsoft.Json;
using System.Net;

public class IPAddressConverter : JsonConverter<IPAddress>
{
    public override void WriteJson(JsonWriter writer, IPAddress value, JsonSerializer serializer)
    {
        writer.WriteValue(value?.ToString());
    }

    public override IPAddress ReadJson(JsonReader reader, Type objectType, IPAddress existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var ipString = (string)reader.Value!;
        return string.IsNullOrEmpty(ipString)! ? null! : IPAddress.Parse(ipString)!;
    }
}
