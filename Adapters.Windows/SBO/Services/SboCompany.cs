using System.Runtime.InteropServices;
using Core.Interfaces;
using Core.Models.Settings;
using SAPbobsCOM;

namespace Adapters.Windows.SBO.Services;

public class SboCompany(ISettings settings) {
    private readonly SboSettings sboSettings = settings.SboSettings ?? throw new InvalidOperationException("SBO settings are not configured.");
    
    public  Mutex       TransactionMutex { get; set; } = new(false, "CompanyTransactionMutex");
    public  Mutex       ConnectionMutex  { get; set; } = new(false, "ConnectionMutex");

    public CompanyClass? Company { get; private set; }

    public bool ConnectCompany() {
        ConnectionMutex.WaitOne();
        Company ??= new CompanyClass {
            Server       = sboSettings.Server,
            DbServerType = (BoDataServerTypes)sboSettings.ServerType,
            CompanyDB    = sboSettings.Database,
            UserName     = sboSettings.User,
            Password     = sboSettings.Password
        };
        try {
            try {
                if (Company is { Connected: true })
                    return true;
            }
            catch (Exception) {
                // ignored
            }

            try {
                int returnCode = Company.Connect();
                if (returnCode != 0) {
                    throw new Exception($"Sbo Adapter Connection Error: {Company.GetLastErrorDescription()} (Code: {returnCode})");
                }
            }
            catch (Exception e) {
                if (e.Message.IndexOf("RPC_E_SERVERFAULT") != -1 || e.Message.IndexOf("-8037") != -1 || e.Message.IndexOf("-105") != -1)
                    Retry();
                else
                    throw new Exception(e.Message);
            }
        }
        finally {
            ConnectionMutex.ReleaseMutex();
        }

        return true;

        void Retry() {
            ReleaseComObject(Company);
            GC.Collect();
            Company = null;
            ConnectCompany();
        }
    }

    public void ReleaseComObject(object o) {
        Marshal.ReleaseComObject(o);
        GC.Collect();
    }
}