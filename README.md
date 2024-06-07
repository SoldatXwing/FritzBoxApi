<h1>FritzBoxApi</h1>
<span>This simple 'API' will log in to your local FritzBox with the given password and URL (the default URL is https://fritz.box) and can return all devices that are in the local network.</span>

<h1>Usage</h1>
<span>This simple approach shows how to initialize the FritzBoxAccesser and get the devices from the FritzBox.</span>
<br/><br/>

```csharp
using FritzBoxApi;
public class Program
{
    private static async Task Main(string[] args)
    {
        FritzBoxAccesser.SetAttributes("password");
        FritzBoxAccesser access = new FritzBoxAccesser();
        var devices = await fritzBoxAccesser.GetAllDevciesInNetworkAsync();

        foreach(Device device in devices)
            Console.WriteLine($"Device: {device.Name}, is active: {device.StateInfo.Active}");
    }
}
```

<span>Specify more details for the accessor:</span>
```csharp
FritzBoxAccesser.SetAttributes(fritzBoxPassword:"password", fritzBoxUrl: "https://192.168.178.1", userName: "fritz1234");
FritzBoxAccesser fritzBoxAccesser = new FritzBoxAccesser();
```
<h1>Info</h1>
<span>
  If you want to set a custom FritzBox URL, make sure to use <code>https://</code>. For example, <code>https://192.168.178.1</code>.
</span>
<br/>
<h1>Disclaimer</h1>
 <span>This tool is only for testing and academic purposes and can only be used where strict consent has been given. Do not use it for illegal purposes! It is the end user’s responsibility to obey all applicable local, state, and federal laws. Developers assume no liability and are not responsible for any misuse or damage caused by this tool and software in general.</span>
 
