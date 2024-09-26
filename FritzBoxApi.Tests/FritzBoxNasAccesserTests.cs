using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FritzBoxApi.Tests
{
    internal class FritzBoxNasAccesserTests
    {
        //No specific tests are possible because the NAS systems are constructed differently
        [Test]
        public async Task GetNasBaseStorageDiskInfo_Success()
        {
            var result = await SetUp.NasAccesser.GetNasStorageDiskInfoAsync();
            Assert.IsNotNull(result);
        }
        [Test]
        public async Task GetNasBaseFoldersAsync_Success()
        {
            var result = await SetUp.NasAccesser.GetNasFoldersAsync();
            Assert.IsNotNull(result);
        }
        [Test]
        public void GetNasFileBytes_WrongPath()
        {
            var exception = Assert.ThrowsAsync<InvalidOperationException>(async () => await SetUp.NasAccesser.GetNasFileBytes("/huhuh/se.jpg"));

            Assert.IsNotNull(exception);
            Assert.IsTrue(exception.Message.Contains("Failed to get file bytes"));
        }
    }
}
