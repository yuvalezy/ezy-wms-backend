using System.Runtime.InteropServices;
using Adapters.Windows.SBO.Services;
using Microsoft.Extensions.Logging;
using SAPbobsCOM;

namespace Adapters.Windows.SBO.Helpers;

public class TransferCancel(SboCompany sboCompany, int docEntry, ILoggerFactory loggerFactory) : IDisposable {
    private StockTransfer? transfer;

    public void Execute() {
        Company? company = null;

        try {
            sboCompany.TransactionMutex.WaitOne();

            try {
                sboCompany.ConnectCompany();
                company = sboCompany.Company!;
                company.StartTransaction();

                transfer = (StockTransfer)company.GetBusinessObject(BoObjectTypes.oStockTransfer);
                if (!transfer.GetByKey(docEntry)) {
                    throw new ArgumentException($"Transfer Entry {docEntry} not found!");
                }

                int retCode = transfer.Cancel();
                if (retCode != 0) {
                    throw new Exception(company.GetLastErrorDescription());
                }

                if (company.InTransaction) {
                    company.EndTransaction(BoWfTransOpt.wf_Commit);
                }
            }
            finally {
                sboCompany.TransactionMutex.ReleaseMutex();
            }
        }
        catch (Exception ex) {
            if (company?.InTransaction == true) {
                company.EndTransaction(BoWfTransOpt.wf_RollBack);
            }
        }
    }

    public void Dispose() {
        if (transfer != null) {
            Marshal.ReleaseComObject(transfer);
            transfer = null;
        }

        GC.Collect();
    }
}