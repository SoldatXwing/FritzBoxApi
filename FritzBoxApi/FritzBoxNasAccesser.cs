﻿using FritzBoxApi.Models.NasModels;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;
namespace FritzBoxApi
{
    public class FritzBoxNasAccesser : BaseAccesser
    {
        public FritzBoxNasAccesser(string fritzBoxPassword, string fritzBoxUrl = "https://fritz.box", string userName = "") => (FritzBoxUrl, Password, FritzUserName) = (fritzBoxUrl, fritzBoxPassword, userName);
        
        public async Task<DiskInfo> GetNasStorageDiskInfoAsync(string path = "/")
        {
            if (!path.StartsWith("/"))
                throw new InvalidOperationException(@"Path has to start with: ""/""");
            var response = await GetNasDirectoryInfoAsync(path);
            if (response?.DiskInfo is not null)
                return response.DiskInfo;

            throw new InvalidOperationException("Disk information is not available.");
        }
        public async Task<List<NasDirectory>> GetNasFoldersAsync(string path = "/")
        {
            if (!path.StartsWith("/"))
                throw new InvalidOperationException(@"Path has to start with: ""/""");
            var response = await GetNasDirectoryInfoAsync(path);
            if (response?.Directories is not null)
                return response.Directories;

            throw new InvalidOperationException("Fodlers are not available.");
        }
        public async Task<List<NasFile>> GetNasFilesAsync(string path = "/")
        {
            if (!path.StartsWith("/"))
                throw new InvalidOperationException(@"Path has to start with: ""/""");
            var response = await GetNasDirectoryInfoAsync(path);
            if (response?.Files is not null)
                return response.Files;

            throw new InvalidOperationException("Files are not available");
        }
        private async Task<FirtzBoxNasResponse> GetNasDirectoryInfoAsync(string path = "/")
        {
            if(!path.StartsWith("/"))
                throw new InvalidOperationException(@"Path has to start with: ""/""");
            if (!IsSidValid)
                await GenerateSessionIdAsync();
            var content = new StringContent($"sid={CurrentSid}&path={path}&limit=10000&sorting=%2Bfilename&c=files&a=browse", Encoding.UTF8, "application/x-www-form-urlencoded");
            var response = HttpRequestFritzBox("/nas/api/data.lua", content, HttpRequestMethod.Post);
            if (response.IsSuccessStatusCode)
                return JsonConvert.DeserializeObject<FirtzBoxNasResponse>(await response.Content.ReadAsStringAsync())!;
            throw new Exception("Failed to fetch nas server");
        }
        public async Task<byte[]> GetNasFileBytes(string path = "/")
        {
            if (!path.StartsWith("/"))
                throw new InvalidOperationException(@"Path has to start with: ""/""");
            if (!IsSidValid)
                await GenerateSessionIdAsync();
            var response = HttpRequestFritzBox($"/nas/api/data.lua?c=pictures&a=get&sid={CurrentSid}&path={path}", null, HttpRequestMethod.Get);
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadAsByteArrayAsync();
            throw new InvalidOperationException("Failed to get file bytes");
        }
        public async Task<HttpResponseMessage> UploadFileAsync(string relativeUrl,string relativeNasUrl, long modificationTime, byte[] fileBytes, string fileName)
        {
            if (!IsSidValid)
                await GenerateSessionIdAsync();
            string boundary = "------------------------" + DateTime.Now.Ticks.ToString("x");

            var fileContent = new ByteArrayContent(fileBytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(DetectMimeType(fileBytes));
            var form = new MultipartFormDataContent(boundary)
            {
                { new StringContent(CurrentSid), "sid" },
                { new StringContent(modificationTime.ToString()), "mtime" },
                { new StringContent(relativeNasUrl), "dir" },
                { new StringContent(""), "ResultScript" },
                { fileContent, "UploadFile", fileName}
            };
            var ts = await form.ReadAsStringAsync();
            var response = FormDataRequestFritzBox(relativeUrl, form, HttpRequestMethod.Post);
            var t = await response.Content.ReadAsStringAsync();
            return response;
        }
        private string DetectMimeType(byte[] fileBytes)
        {
            if (fileBytes.Length > 0)
            {
                if (fileBytes[0] == 0x25 && fileBytes[1] == 0x50 && fileBytes[2] == 0x44 && fileBytes[3] == 0x46) // %PDF
                    return "application/pdf";
                else if (fileBytes.Length >= 4 && fileBytes[0] == 0x89 && fileBytes[1] == 0x50 && fileBytes[2] == 0x4E && fileBytes[3] == 0x47) // PNG
                    return "image/png";
                else if (fileBytes.Length >= 2 && fileBytes[0] == 0xFF && fileBytes[1] == 0xD8) // JPEG
                    return "image/jpeg";
            }

            return "application/octet-stream";
        }
        public HttpResponseMessage FormDataRequestFritzBox(string relativeUrl, MultipartFormDataContent? formData, HttpRequestMethod method)
        {
            using (var handler = new HttpClientHandler())
            {
                handler.ServerCertificateCustomValidationCallback = (request, cert, chain, errors) => true;

                using (var httpClient = new HttpClient(handler) { BaseAddress = new Uri(FritzBoxUrl) })
                {
                    if (method is HttpRequestMethod.Post)
                    {
                        var response = httpClient.PostAsync(relativeUrl, formData)
                            .GetAwaiter()
                            .GetResult();
                        return response;
                    }
                    throw new NotImplementedException("Only Post method is supported!");
                }
            }
        }

    }

}

