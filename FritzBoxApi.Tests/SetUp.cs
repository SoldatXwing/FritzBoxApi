using Microsoft.Extensions.Configuration;
using System.IO;

namespace FritzBoxApi.Tests
{
    [SetUpFixture]
    internal class SetUp
    {
        public static FritzBoxAccesser FritzBoxAccesser { get; private set; }
        public static FritzBoxNasAccesser NasAccesser { get; private set; }
        public static IConfiguration Config { get; }
        static SetUp()
        {
            Config = new ConfigurationBuilder()
                        .SetBasePath(AppContext.BaseDirectory)
                        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)  // Ensure appsettings.json is present
                        .Build();
        }
        [OneTimeSetUp]
        public void SetUpAsync()
        {
            IConfigurationSection fritzBoxConfig = Config.GetSection("fritzBox");
            string password = fritzBoxConfig["password"]!;
            string fritzUserName = fritzBoxConfig["fritzUserName"]!;
            string fritzBoxUrl = fritzBoxConfig["fritzUrl"]!;

            FritzBoxAccesser = new FritzBoxAccesser(password, fritzBoxUrl, fritzUserName);
            NasAccesser = new FritzBoxNasAccesser(password, fritzBoxUrl, fritzUserName);
        }
    }
}