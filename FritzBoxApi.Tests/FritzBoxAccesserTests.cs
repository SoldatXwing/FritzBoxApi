using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FritzBoxApi.Tests
{
    internal class FritzBoxAccesserTests
    {
        [Test]
        public async Task GetAllDevicesInNetworkAsync_Success()
        {
            List<Device> devices = await SetUp.FritzBoxAccesser.GetAllDevciesInNetworkAsync();  
            Assert.IsNotNull(devices);
            Assert.That(devices.Count, Is.GreaterThan(0));
        }
        [Test]
        public async Task GetAllDevicesInNetworkAsyncWithIpAndUid_Success()
        {
            List<Device> devices = await SetUp.FritzBoxAccesser.GetAllDevciesInNetworkAsync(true);
            Assert.IsNotNull(devices);
            Assert.That(devices.Count, Is.GreaterThan(0));
        }
        [Test]
        public void ChangeInternetAccessState_EmptyParameters()
        {
            var exception = Assert.ThrowsAsync<NotImplementedException>(async () =>
                await SetUp.FritzBoxAccesser.ChangeInternetAccessStateForDeviceAsync(
                    "",
                    Models.InternetDetail.Unlimited,
                    new System.Net.IPAddress(new byte[] {2,2,2,2}),
                    ""
                )
            );
            Assert.IsNotNull(exception);
            Assert.IsTrue(exception.Message.Contains("Parameters cant be empty or null!"));
        }
    }
}
