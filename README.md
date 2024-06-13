<h1>FritzBoxApi</h1>
<span>This simple 'API' will log in to your local FritzBox with the given password and URL (the default URL is https://fritz.box) and can return all devices that are in the local network, and much more.</span>

<h1>Usage</h1>
<span>This simple approach shows how to initialize the FritzBoxAccesser and get the devices from the FritzBox.</span>
<br/><br/>

```csharp
using FritzBoxApi;
public class Program
{
    private static async Task Main(string[] args)
    {        
        FritzBoxAccesser fritzBoxAccesser = new FritzBoxAccesser(fritzBoxPassword: "password");
        var devices = await fritzBoxAccesser.GetAllDevciesInNetworkAsync();

        foreach(Device device in devices)
            Console.WriteLine($"Device: {device.Name}, is active: {device.StateInfo.Active}");
    }
}
```

<span>Specify more details for the access:</span>
```csharp
FritzBoxAccesser fritzBoxAccesser = new FritzBoxAccesser(fritzBoxPassword: "password", fritzBoxUrl: "https://192.168.178.1", userName: "fritz3000");
```
<br/>
<span>
    To change a device's internet access state, do the following:
</span>

```csharp
using FritzBoxApi;
public class Program
{
    private static async Task Main(string[] args)
    {
        FritzBoxAccesser fritzBoxAccesser = new FritzBoxAccesser(fritzBoxPassword: "password");
        var device = await fritzBoxAccesser.GetSingleDeviceJTokenAsync(deviceName: "DESKTOP123");
        await fritzBoxAccesser.ChangeInternetAccessStateForDevice(
                device["name"]?.ToString()!,
                InternetDetail.Unlimited,
                IPAddress.Parse(device["ip"]?.ToString()!),
                device["uid"]?.ToString()!
        );

    }
}
```
<h1>Info</h1>
<span>
  If you want to set a custom FritzBox URL, make sure to use <code>https://</code>. For example, <code>https://192.168.178.1</code>.
</span>
<br/><br/>
<span>The following benchmark shows the performance difference between the GetAllDevicesInNetworkAsync method with the getWithIp parameter set to true and false.</span>

```
| Method              | Mean    | Error    | StdDev   |
|-------------------- |--------:|---------:|---------:|
| GetDevicesWithoutIp | 4.451 s | 0.0881 s | 0.1633 s |
| GetDevicesWithIp    | 6.855 s | 0.1362 s | 0.2160 s |
```


<br/>
<h1>Disclaimer</h1>
 <span>This tool is only for testing and academic purposes and can only be used where strict consent has been given. Do not use it for illegal purposes! It is the end user’s responsibility to obey all applicable local, state, and federal laws. Developers assume no liability and are not responsible for any misuse or damage caused by this tool and software in general.</span>
 
