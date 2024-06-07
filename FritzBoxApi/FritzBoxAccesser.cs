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

    public static void SetAttributes(string fritzBoxPassword, string fritzBoxUrl = "https://fritz.box",string userName = "") => (FritzBoxUrl, Password, fritzUserName) = (fritzBoxUrl, fritzBoxPassword, userName);
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
                    return sid;

                var challenge = xml.Root.Element("Challenge").Value;
                fritzUserName = fritzUserName is "" ? xml.Root.Element("Users")?.Element("User").Value! : fritzUserName;

                var responseHash = CalculateMD5(challenge + "-" + Password);
                var content = new StringContent($"response={challenge}-{responseHash}&username={fritzUserName}&lp=overview&loginView=simple", Encoding.UTF8, "application/x-www-form-urlencoded");


                var loginResponse = await client.PostAsync("/login_sid.lua", content);
                var loginResponseContent = await loginResponse.Content.ReadAsStringAsync();
                var loginXml = XDocument.Parse(loginResponseContent);
                var loginSid = loginXml.Root.Element("SID").Value;


                if (loginSid == "0000000000000000")
                    throw new Exception("Login failed. Ensure (if set) username and password is correct!");
                

                return loginSid;
            }
        }
    }
    public async Task<string> GetOverViewPageJsonAsync()
    {
            var sid = await GetSessionId();
            using (var handler = new HttpClientHandler())
            {
                handler.ServerCertificateCustomValidationCallback = (request, cert, chain, errors) => true;
                HttpClient client = new HttpClient(handler) { BaseAddress = new Uri(FritzBoxUrl) };
                var content = new StringContent($"xhr=1&sid={sid}&lang=de&page=overview&xhrId=all&useajax=1&no_sidrenew=", Encoding.UTF8, "application/x-www-form-urlencoded");
                var response = client.PostAsync("/data.lua", content)
                    .GetAwaiter()
                    .GetResult();
                if (response.IsSuccessStatusCode)
                    return await response.Content.ReadAsStringAsync();

                throw new Exception("Failed to fetch fritzbox overview page json");
            }
        
    }

    public async Task<List<Device>> GetAllDevciesInNetworkAsync() => JsonConvert.DeserializeObject<FritzBoxResponse>(await GetOverViewPageJsonAsync())!.Data.Net.Devices!;
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



