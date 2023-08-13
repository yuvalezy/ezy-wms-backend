using System;
using BE1S.Global;
using LMService;
using NUnit.Framework;

namespace LM.Service.UnitTest {
    public abstract class Session {
        public LMService.Service Service { get; private set; }

        [SetUp, Order(1)]
        public void Connect() {
            Service                       = new LMService.Service();
            Global.Service                = Service;
            ConnectionController.Database = System.Configuration.ConfigurationManager.AppSettings.Get("dbName");
            Assert.That(!string.IsNullOrWhiteSpace(ConnectionController.Database));
            Global.LoadRegistrySettings();
            Global.LoadDatabaseSettings();
            Global.LoadRestAPISettings();
        }

        protected static void BeginTransaction() => ConnectionController.BeginTransaction();
        protected static void Rollback()         => ConnectionController.Rollback();

        protected static DateTime GetDate(int hour, int minute) {
            var date = DateTime.Now.Date;
            date = date.AddHours(hour);
            date = date.AddMinutes(minute);
            return date;
        }
    }
}