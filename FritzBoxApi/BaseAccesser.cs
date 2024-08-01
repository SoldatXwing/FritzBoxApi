using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml;

namespace FritzBoxApi
{
    public abstract class BaseAccesser : IAccesser
    {
        protected static string FritzBoxUrl = string.Empty;
        protected string Password = string.Empty;
        protected string fritzUserName;
        public string CalculateMD5(string input)
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
        public async Task<string> GetSessionIdAsync()
        {
            try
            {
                var response = HttpRequestFritzBox("/login_sid.lua", null, HttpRequestMethod.Get);
                var t = await response.Content.ReadAsStringAsync();
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
            catch (XmlException ex)
            {
                throw new XmlException("Failed to parse xml page. Try a different fritzbox url.");
            }
        }
        public HttpResponseMessage HttpRequestFritzBox(string relativeUrl, StringContent? bodyParameters, HttpRequestMethod method)
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
    }
}
