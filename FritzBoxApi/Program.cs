using FritzBoxApi.Models;
using Newtonsoft.Json;
using System.Net.Http.Headers;
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

    public static void SetAttributes(string fritzBoxPassword, string fritzBoxUrl = "https://fritz.box") => (FritzBoxUrl, Password) = (fritzBoxUrl, fritzBoxPassword);
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
        using (var handler = new HttpClientHandler())
        {
            handler.ServerCertificateCustomValidationCallback = (request, cert, chain, errors) => true;
            using (var client = new HttpClient(handler) { BaseAddress = new Uri(FritzBoxUrl) })
            {
                var response = await client.GetStringAsync("/login_sid.lua");
                var xml = XDocument.Parse(response);
                var sid = xml.Root.Element("SID").Value;
                if (sid != "0000000000000000")
                {
                    return sid;
                }

                var challenge = xml.Root.Element("Challenge").Value;
                fritzUserName = xml.Root.Element("Users")?.Element("User").Value;
                var responseHash = CalculateMD5(challenge + "-" + Password);
                var content = new StringContent($"response={challenge}-{responseHash}&username={fritzUserName}&lp=overview&loginView=simple", Encoding.UTF8, "application/x-www-form-urlencoded");


                var loginResponse = await client.PostAsync("/login_sid.lua", content);
                var loginResponseContent = await loginResponse.Content.ReadAsStringAsync();
                var loginXml = XDocument.Parse(loginResponseContent);
                var loginSid = loginXml.Root.Element("SID").Value;


                if (loginSid == "0000000000000000")
                {
                    throw new Exception();
                }

                return loginSid;
            }
        }
    }
    public async Task<string> GetOverViewPageJson()
    {
        try
        {
            var sid = await GetSessionId();
            using (var handler = new HttpClientHandler())
            {
                handler.ServerCertificateCustomValidationCallback = (request, cert, chain, errors) => true;
                HttpClient client = new HttpClient(handler) { BaseAddress = new Uri(FritzBoxUrl) };

                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/125.0.6422.112 Safari/537.36");
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
                client.DefaultRequestHeaders.Add("Origin", FritzBoxUrl);
                client.DefaultRequestHeaders.Referrer = new Uri(FritzBoxUrl);
                client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
                client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
                client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("br"));
                client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("de-DE"));
                client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("de", 0.9));
                client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en-US", 0.8));
                client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("en", 0.7));
                client.DefaultRequestHeaders.Connection.Add("keep-alive");

                var content = new StringContent($"xhr=1&sid={sid}&lang=de&page=overview&xhrId=all&useajax=1&no_sidrenew=", Encoding.UTF8, "application/x-www-form-urlencoded");
                var response = client.PostAsync("/data.lua", content)
                    .GetAwaiter()
                    .GetResult();
                if (response.IsSuccessStatusCode)
                    return await response.Content.ReadAsStringAsync();

                throw new Exception("Failed to fetch fritzbox overview page json");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return string.Empty;
        }
    }

    public async Task<FritzBoxResponse> GetAllDevciesInNetwork() => JsonConvert.DeserializeObject<FritzBoxResponse>(await GetOverViewPageJson())!;
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
    public async Task<string> GetDeviceInfo(string sessionId, string options) => await ExecuteCommand(sessionId, "getdevicelistinfos", null, options);
    private async Task<string> ExecuteCommand(string sid, string command, string ain, string path)
    {
        path += "/webservices/homeautoswitch.lua?0=0";
        if (!string.IsNullOrEmpty(sid))
            path += "&sid=" + sid;
        if (!string.IsNullOrEmpty(command))
            path += "&switchcmd=" + command;
        if (!string.IsNullOrEmpty(ain))
            path += "&ain" + ain;
        return await HttpRequest(path, new HttpRequestMessage(), "");
    }
    private async Task<string> HttpRequest(string path, HttpRequestMessage req, string options)
    {
        req.RequestUri = new Uri(FritzBoxUrl + path);
        var client = new HttpClient();

        HttpResponseMessage response = await client.SendAsync(req);
        string body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.Redirect || body.Contains("action=.?login.lua"))
        {
            throw new HttpRequestException($"HTTP request failed: {response.StatusCode}");
        }

        return body.Trim();
    }
    public async Task<List<string>> GetSwitchList(string sid, string options)
    {
        string res = await ExecuteCommand(sid, "getswitchlist", null, "");

        // Erzwinge leeres Array bei leerem Ergebnis
        return string.IsNullOrEmpty(res) ? new List<string>() : new List<string>(res.Split(','));
    }
}



