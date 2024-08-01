public interface IAccesser
{
    string CalculateMD5(string input);
    HttpResponseMessage HttpRequestFritzBox(string relativeUrl, StringContent? bodyParameters, HttpRequestMethod method);
    Task<string> GetSessionIdAsync();
}



