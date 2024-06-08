using FritzBoxApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;

public class FritzBoxAccesser
{
    private static string FritzBoxUrl = string.Empty;
    private static string Password = string.Empty;
    private static string fritzUserName;
    private readonly string path = "/net/home_auto_hkr_edit.lua";
    private static readonly HttpClient _HttpClient = new HttpClient();

    public static void SetAttributes(string fritzBoxPassword, string fritzBoxUrl = "https://fritz.box", string userName = "") => (FritzBoxUrl, Password, fritzUserName) = (fritzBoxUrl, fritzBoxPassword, userName);
    public FritzBoxAccesser()
    {
        if (Password is "" || FritzBoxUrl is "")
            throw new NotImplementedException("Password or firtzbox url is not set! Set these by calling the SetAttributes() mehtod");
    }
    static FritzBoxAccesser()
    {

    }
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
    public async Task<List<Device>> ResolveIpsForDevices(List<Device> devices)
    {
        var sid = await GetSessionId();
        var content = new StringContent($"xhr=1&sid={sid}&lang=de&page=wSet&xhrId=all", Encoding.UTF8, "application/x-www-form-urlencoded");
        var response = HttpRequestFritzBox("/data.lua", content, HttpRequestMethod.Post);
        if (response.IsSuccessStatusCode)
        {
            JObject jsonObject = JObject.Parse(await response.Content.ReadAsStringAsync());
            JToken knownWlanDevicesToken = jsonObject["data"]["wlanSettings"]["knownWlanDevices"];

            List<KnownWlanDevice> knownWlanDevices = knownWlanDevicesToken.ToObject<List<KnownWlanDevice>>();
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
        throw new Exception("Failed to fetch fritzbox overview page json");
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
                throw new NotImplementedException("Only Put and Post methods are supported!");
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



