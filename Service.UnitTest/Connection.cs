using BE1S.Global;
using LMService;
using NUnit.Framework;

namespace LM.Service.UnitTest {
    [TestFixture]
    public class Connection : Session {
        [Test, Order(1)]
        public void Connect() {
            Assert.That(!string.IsNullOrWhiteSpace(ConnectionController.Server));
            Assert.That(Global.ServerType != 0);
            Assert.That(!string.IsNullOrWhiteSpace(ConnectionController.DbServerUser));
            Assert.That(!string.IsNullOrWhiteSpace(ConnectionController.DbServerPassword));
            Assert.That(!string.IsNullOrWhiteSpace(Global.LicenseServer));
            Assert.That(!string.IsNullOrWhiteSpace(Global.DBServiceVersion));
        }
    }
}