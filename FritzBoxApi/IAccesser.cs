public interface IAccesser
{
    HttpResponseMessage HttpRequestFritzBox(string relativeUrl, StringContent? bodyParameters, HttpRequestMethod method);
    Task<bool> GenerateSessionIdAsync();
}



