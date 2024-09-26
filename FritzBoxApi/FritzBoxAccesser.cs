using FritzBoxApi;
using FritzBoxApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Text;

public class FritzBoxAccesser : BaseAccesser
{
    public FritzBoxAccesser(string fritzBoxPassword, string fritzBoxUrl = "https://fritz.box", string userName = "") => (FritzBoxUrl, Password, FritzUserName) = (fritzBoxUrl, fritzBoxPassword, userName);
    private async Task<string> GetOverViewPageJsonAsync()
    {
        if (!IsSidValid)
            await GenerateSessionIdAsync();
        var content = new StringContent($"xhr=1&sid={CurrentSid}&lang=de&page=overview&xhrId=all&useajax=1&no_sidrenew=", Encoding.UTF8, "application/x-www-form-urlencoded");
        var response = HttpRequestFritzBox("/data.lua", content, HttpRequestMethod.Post);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadAsStringAsync();
        throw new Exception("Failed to fetch fritzbox overview page json");
    }
    private async Task<string> GetWifiRadioNetworkPageJsonAsync()
    {
        if(!IsSidValid)
            await GenerateSessionIdAsync();
        var content = new StringContent($"xhr=1&sid={CurrentSid}&lang=de&page=wSet&xhrId=all", Encoding.UTF8, "application/x-www-form-urlencoded");
        var response = HttpRequestFritzBox("/data.lua", content, HttpRequestMethod.Post);
        if (response.IsSuccessStatusCode)
            return await response.Content.ReadAsStringAsync();
        throw new Exception("Failed to fetch fritzbox Wifi radio network page json");
    }
    public async Task<List<Device>> ResolveIpsAndUidForDevices(List<Device> devices)
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
                {
                    c.Ip = IPAddress.Parse(matchingDevice.Ip);
                    c.Uid = matchingDevice.Uid;
                }
            }
            catch (InvalidOperationException) //catches if more than 1 "known" device is found, and now search in the active ones
            {
                var matchingDevice = knownWlanDevices.Where(c => c.Type == "active").SingleOrDefault(d => d.Name == c.Name);
                if (matchingDevice is not null)
                {
                    c.Ip = IPAddress.Parse(matchingDevice.Ip);
                    c.Uid = matchingDevice.Uid;
                }
            }

        });
        return devices;

    }
    /// <summary>
    /// Task to get all active devices in local Network. Note: if getWithIp is enabled, the Task takes more time.
    /// </summary>
    /// <param name="getWithIpAndUid">Bool if the ip attribut should be filled</param>
    /// <returns>A list of Devices</returns>
    public async Task<List<Device>> GetAllDevciesInNetworkAsync(bool getWithIpAndUid = false)
    {
        var result = JsonConvert.DeserializeObject<FritzBoxResponse>(await GetOverViewPageJsonAsync())!.Data.Net.Devices!;
        if (!getWithIpAndUid)
            return result;
        return await ResolveIpsAndUidForDevices(result);
    }
    public async Task<Device> GetSingleDeviceAsync(string deviceName)
    {
        var response = JObject.Parse(await GetWifiRadioNetworkPageJsonAsync());
        var deviceJson = response["data"]?["wlanSettings"]?["knownWlanDevices"]
                ?.FirstOrDefault(d => d["name"]?.ToString() == deviceName)
                ?.ToString();

        if (deviceJson is null)
            throw new KeyNotFoundException($"No Device with name: {deviceName} found!");
        return JsonConvert.DeserializeObject<Device>(deviceJson)!;
    }
    public async Task<Device> GetSingleDeviceAsync(IPAddress ip)
    {
        var response = JObject.Parse(await GetWifiRadioNetworkPageJsonAsync());
        var deviceJson = response["data"]?["wlanSettings"]?["knownWlanDevices"]
                        ?.FirstOrDefault(d => d["ip"]?.ToString() == ip.ToString())
                        ?.ToString();

        if (deviceJson is null)
            throw new KeyNotFoundException($"No Device with ip: {ip} found!");
        return JsonConvert.DeserializeObject<Device>(deviceJson)!;
    }
    /// <summary>
    /// Method to change a access state for a device in local network.
    /// </summary>
    /// <param name="devName">Devicename aka "name"</param>
    /// <param name="internetDetailState">Internet state for device</param>
    /// <param name="ipAdress">Ip from device</param>
    /// <param name="dev">Dev from device, aka "uid"</param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException">Paramters cant be empty or null</exception>
    /// <exception cref="ArgumentException">Invalid ip adress format</exception>
    public async Task ChangeInternetAccessStateForDeviceAsync(string devName, InternetDetail internetDetailState, IPAddress ipAdress, string dev)
    {
        if (string.IsNullOrEmpty(devName) ||
            string.IsNullOrEmpty(dev) ||
            ipAdress is null)
            throw new NotImplementedException("Paramters cant be empty or null!");
        try
        {
            var sid = await GenerateSessionIdAsync();
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
    /// <summary>
    /// Method to change a access state for a device in local network.
    /// </summary>
    /// <param name="device">Device with given properties</param>
    /// <param name="internetDetailState">Internet state for device</param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException">Paramters cant be empty or null</exception>
    /// <exception cref="ArgumentException">Invalid ip adress format</exception>
    public async Task ChangeInternetAccessStateForDeviceAsync(Device device, InternetDetail internetDetailState)
    {
        if (string.IsNullOrEmpty(device.Name) ||
            string.IsNullOrEmpty(device.Uid) ||
            device.Ip is null)
            throw new NotImplementedException("Paramters cant be empty or null!");
        try
        {
            var sid = await GenerateSessionIdAsync();
            var interFaceResponse = HttpRequestFritzBox("/data.lua", new StringContent($"xhr=1&sid={sid}&lang=de&page=edit_device&xhrId=all&dev={device.Uid}&back_to_page=wSet", Encoding.UTF8, "application/x-www-form-urlencoded"), HttpRequestMethod.Post);
            var iFaceIdJson = JObject.Parse(await interFaceResponse.Content.ReadAsStringAsync());
            string interFaceId = string.Empty;
            //IPv6
            if (bool.Parse(iFaceIdJson["data"]!["vars"]!["ipv6_enabled"]!.ToString()))
                interFaceId = iFaceIdJson["data"]!["vars"]!["dev"]!["ipv6"]!["iface"]!["ifaceid"]!.ToString();
            else //IPv4
                throw new Exception("Unable to get interfaceid from device!");

            string[] interFaceParts = interFaceId.Split(':');
            string[] ipOctets = device.Ip.ToString().Split('.');
            if (ipOctets.Length != 4)
                throw new ArgumentException("Invalid IP address format");

            var bodyParamters = new StringContent(
                $"xhr=1&dev_name={device.Name}&internetdetail={internetDetailState.ToString().ToLower()}&allow_pcp_and_upnp=off&dev_ip0={ipOctets[0]}&dev_ip1={ipOctets[1]}&dev_ip2={ipOctets[2]}&dev_ip3={ipOctets[3]}&dev_ip={device.Ip.ToString()}&static_dhcp=off&interface_id1={interFaceParts[2]}&interface_id2={interFaceParts[3]}&interface_id3={interFaceParts[4]}&interface_id4={interFaceParts[5]}&back_to_page=wSet&dev={device.Uid}&apply=true&sid={sid}&lang=de&page=edit_device",
                Encoding.UTF8,
                "application/x-www-form-urlencoded"
                );

            var response = HttpRequestFritzBox("/data.lua", bodyParamters, HttpRequestMethod.Post);
            if (!response.IsSuccessStatusCode)
                throw new Exception($"Error blocking internet access for device {device.Name}. Ensure all parameters are correct");
        }
        catch
        {
            throw new ArgumentException("Invalid IP address format");
        }


    }
    /// <summary>
    /// Reconnect router to recieve a new IP adress from provider
    /// </summary>
    /// <returns></returns>
    /// <exception cref="Exception">Throws if fails</exception>
    public async Task ReconnectAsync()
    {
        var sid = await GenerateSessionIdAsync();
        var content = new StringContent($"xhr=1&sid={sid}&lang=de&page=netMoni&xhrId=reconnect&disconnect=true&useajax=1&no_sidrenew=", Encoding.UTF8, "application/x-www-form-urlencoded");
        var response = HttpRequestFritzBox("/data.lua", content, HttpRequestMethod.Post);
        Thread.Sleep(1000);
        content = new StringContent($"xhr=1&sid=dac944fb519e10d6&lang=de&page=netMoni&xhrId=reconnect&connect=true&useajax=1&no_sidrenew=");      
        var secondResponse = HttpRequestFritzBox("/data.lua",content, HttpRequestMethod.Post);
        if(!response.IsSuccessStatusCode && secondResponse.IsSuccessStatusCode)
            throw new Exception("Error reconnecting frit box!");
    }

}




