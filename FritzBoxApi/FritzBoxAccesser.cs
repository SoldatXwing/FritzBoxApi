using FritzBoxApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;

public class FritzBoxAccesser
{
    private static string FritzBoxUrl = string.Empty;
    private static string Password = string.Empty;
    private static string fritzUserName;
    public FritzBoxAccesser(string fritzBoxPassword, string fritzBoxUrl = "https://fritz.box", string userName = "") => (FritzBoxUrl, Password, fritzUserName) = (fritzBoxUrl, fritzBoxPassword, userName);
    private async Task<string> GetSessionId()
    {
        var response = HttpRequestFritzBox("/login_sid.lua", null, HttpRequestMethod.Get);
        var xml = XDocument.Parse(await response.Content.ReadAsStringAsync());
        var sid = xml.Root.Element("SID").Value;
        if (sid != "0000000000000000")
            return sid;

        var challenge = xml.Root.Element("Challenge").Value;
        fritzUserName = fritzUserName is "" ? xml.Root.Element("Users")?.Element("User").Value! : fritzUserName;

        var responseHash = CalculateMD5(challenge + "-" + Password);
        var content = new StringContent($"response={challenge}-{responseHash}&username={fritzUserName}&lp=overview&loginView=simple", Encoding.UTF8, "application/x-www-form-urlencoded");

        var loginResponse = HttpRequestFritzBox("/login_sid.lua", content, HttpRequestMethod.Post);
        var loginResponseContent = await loginResponse.Content.ReadAsStringAsync();
        var loginXml = XDocument.Parse(loginResponseContent);
        var loginSid = loginXml.Root.Element("SID").Value;

        if (loginSid == "0000000000000000")
            throw new Exception("Login failed. Ensure (if set) username and password is correct!");

        return loginSid;
    }
    public async Task<string> GetOverViewPageJsonAsync()
    {
        var sid = await GetSessionId();
        var content = new StringContent($"xhr=1&sid={sid}&lang=de&page=overview&xhrId=all&useajax=1&no_sidrenew=", Encoding.UTF8, "application/x-www-form-urlencoded");
        var response = HttpRequestFritzBox("/data.lua", content, HttpRequestMethod.Post);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadAsStringAsync();

        throw new Exception("Failed to fetch fritzbox overview page json");
    }
    public async Task<string> GetWifiRadioNetworkPageJsonAsync()
    {
        var sid = await GetSessionId();
        var content = new StringContent($"xhr=1&sid={sid}&lang=de&page=wSet&xhrId=all", Encoding.UTF8, "application/x-www-form-urlencoded");
        var response = HttpRequestFritzBox("/data.lua", content, HttpRequestMethod.Post);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadAsStringAsync();
        throw new Exception("Failed to fetch fritzbox Wifi radio network page json");
    }
    public async Task<List<Device>> ResolveIpsForDevices(List<Device> devices)
    {
        var response = await GetWifiRadioNetworkPageJsonAsync();
        JObject jsonObject = JObject.Parse(response);
        JToken knownWlanDevicesToken = jsonObject["data"]!["wlanSettings"]!["knownWlanDevices"]!;

        List<KnownWlanDevice> knownWlanDevices = knownWlanDevicesToken.ToObject<List<KnownWlanDevice>>()!;
        devices.ForEach(c =>
        {
            try
            {
                var matchingDevice = knownWlanDevices.SingleOrDefault(d => d.Name == c.Name);
                if (matchingDevice is not null)
                    c.Ip = matchingDevice.Ip;
            }
            catch (InvalidOperationException) //catches if more than 1 "known" device is found, and now search in the active ones
            {
                var matchingDevice = knownWlanDevices.Where(c => c.Type == "active").SingleOrDefault(d => d.Name == c.Name);
                if (matchingDevice is not null)
                    c.Ip = matchingDevice.Ip;
            }

        });
        return devices;

    }
    /// <summary>
    /// Task to get all active devices in local Network. Note: if getWithIp is enabled, the Task takes more time.
    /// </summary>
    /// <param name="getWithIp">Bool if the ip attribut should be filled</param>
    /// <returns>A list od Devices</returns>
    public async Task<List<Device>> GetAllDevciesInNetworkAsync(bool getWithIp = false)
    {
        var result = JsonConvert.DeserializeObject<FritzBoxResponse>(await GetOverViewPageJsonAsync())!.Data.Net.Devices!;
        if (!getWithIp)
            return result;
        return await ResolveIpsForDevices(result);
    }
    public async Task<JToken> GetSingleDeviceJTokenAsync(string deviceName)
    {
        var response = JObject.Parse(await GetWifiRadioNetworkPageJsonAsync());
        return response["data"]?["wlanSettings"]?["knownWlanDevices"]?.FirstOrDefault(d => d["name"]?.ToString() == "0b98ddcc-a57f-4898-9035-a678ce5692a0")!;
    }
    public async Task ChangeInternetAccessStateForDevice(string devName, InternetDetail internetDetailState, IPAddress ipAdress, string dev)
    {
        if (string.IsNullOrEmpty(devName) ||
            string.IsNullOrEmpty(dev) ||
            ipAdress is null)
            throw new NotImplementedException("Paramters cant be empty or null!");
        try
        {
            var sid = await GetSessionId();
            var interFaceResponse = HttpRequestFritzBox("/data.lua", new StringContent($"xhr=1&sid={sid}&lang=de&page=edit_device&xhrId=all&dev={dev}&back_to_page=wSet", Encoding.UTF8, "application/x-www-form-urlencoded"), HttpRequestMethod.Post);
            var iFaceIdJson = JObject.Parse(await interFaceResponse.Content.ReadAsStringAsync());
            string interFaceId = string.Empty;
            //IPv6
            if (bool.Parse(iFaceIdJson["data"]!["vars"]!["ipv6_enabled"]!.ToString()))
                interFaceId = iFaceIdJson["data"]!["vars"]!["dev"]!["ipv6"]!["iface"]!["ifaceid"]!.ToString();
            else //IPv4
                throw new Exception("Unable to get interfaceid from device!");

            string[] interFaceParts = interFaceId.Split(':');
            string[] ipOctets = ipAdress.ToString().Split('.');
            if (ipOctets.Length != 4)
                throw new ArgumentException("Invalid IP address format");

            var bodyParamters = new StringContent(
                $"xhr=1&dev_name={devName}&internetdetail={internetDetailState.ToString().ToLower()}&allow_pcp_and_upnp=off&dev_ip0={ipOctets[0]}&dev_ip1={ipOctets[1]}&dev_ip2={ipOctets[2]}&dev_ip3={ipOctets[3]}&dev_ip={ipAdress}&static_dhcp=off&interface_id1={interFaceParts[2]}&interface_id2={interFaceParts[3]}&interface_id3={interFaceParts[4]}&interface_id4={interFaceParts[5]}&back_to_page=wSet&dev={dev}&apply=true&sid={sid}&lang=de&page=edit_device",
                Encoding.UTF8,
                "application/x-www-form-urlencoded"
                );

            var response = HttpRequestFritzBox("/data.lua", bodyParamters, HttpRequestMethod.Post);
            if (!response.IsSuccessStatusCode)
                throw new Exception($"Error blocking internet access for device {devName}. Ensure all parameters are correct");
        }
        catch
        {
            throw new ArgumentException("Invalid IP address format");
        }


    }
    private HttpResponseMessage HttpRequestFritzBox(string relativeUrl, StringContent? bodyParameters, HttpRequestMethod method)
    {
        using (var handler = new HttpClientHandler())
        {
            handler.ServerCertificateCustomValidationCallback = (request, cert, chain, errors) => true;
            using (var httpClient = new HttpClient(handler) { BaseAddress = new Uri(FritzBoxUrl) })
            {
                if (method is HttpRequestMethod.Post)
                {
                    var response = httpClient.PostAsync(relativeUrl, bodyParameters)
                        .GetAwaiter()
                        .GetResult();
                    return response;
                }
                else if (method is HttpRequestMethod.Get)
                {
                    var response = httpClient.GetAsync(relativeUrl)
                        .GetAwaiter()
                        .GetResult();
                    return response;
                }
                throw new NotImplementedException("Only Get and Post methods are supported!");
            }
        }
    }
    private string CalculateMD5(string input)
    {
        using (MD5 md5 = MD5.Create())
        {
            byte[] inputBytes = Encoding.Unicode.GetBytes(input);
            byte[] hashBytes = md5.ComputeHash(inputBytes);

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hashBytes.Length; i++)
            {
                sb.Append(hashBytes[i].ToString("x2"));
            }
            return sb.ToString();
        }
    }
}



