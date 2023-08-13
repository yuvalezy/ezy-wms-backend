using System;
using System.Windows.Forms;
namespace Service.Crystal.Shared; 

public abstract class CrystalLayout : IDisposable {
    public abstract void                    Load(string                fileName);
    public abstract void                    SetConnection(DatabaseType dbType, string server, string db, string user, string password);
    public abstract bool                    HasParameters { get; }
    public abstract CrystalLayoutParameters GetParameters();
    public abstract void                    SetParameter(string id, object value);
    public abstract void                    SetPrinter(string   name);
    public abstract void                    Print(string        printerName, int copies);
    public abstract bool                    ExportPDF(string    filePath);
    public abstract void                    SetViewer(Form      form);

    protected void            PrintClick(object sender, EventArgs e) => PrintClickEventHandler.Invoke(sender, e);
    public event EventHandler PrintClickEventHandler;
    public abstract void      Dispose();
}
public enum DatabaseType {
    /// <summary>
    /// Microsoft SQL Server
    /// </summary>
    SQL = 0,

    /// <summary>
    /// SAP HANA Server
    /// </summary>
    HANA = 1
}