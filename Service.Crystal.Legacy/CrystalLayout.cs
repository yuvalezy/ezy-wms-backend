using System.Windows.Forms;
using Service.Crystal.Shared;
using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.ReportAppServer.Controllers;
using CrystalDecisions.Shared;
using CrystalDecisions.Windows.Forms;
using System.Text;
using System;

namespace Service.Crystal.Legacy; 

public class CrystalLayout : Service.Crystal.Shared.CrystalLayout {
    private readonly ReportDocument     doc                = new();
    private readonly PrintReportOptions printReportOptions = new();

    public override void Load(string fileName) => doc.Load(fileName);

    public override void SetConnection(DatabaseType dbType, string server, string db, string user, string password) {
        try {
            switch (dbType) {
                case DatabaseType.SQL:
                    SetSQLConnection(server, db, user, password);
                    break;
                case DatabaseType.HANA:
                    SetHANAConnection(server, db, user, password);
                    break;
            }
        }
        catch {
            // ignored
        }
    }


    private void SetSQLConnection(string server, string db, string user, string password) {
        foreach (IConnectionInfo connection in doc.DataSourceConnections) {
            connection.SetConnection(server, db, user, password);
        }
    }
    private void SetHANAConnection(string server, string db, string user, string password) {
        var builder = new StringBuilder();
        builder.Append("driver={B1CRHPROXY");
        if (!Environment.Is64BitProcess)
            builder.Append("32");
        builder.Append("};");
        if (server.Contains("@")) {
            var arrServer = server.Split('@');
            builder.Append($"ServerNode={arrServer[1]};DatabaseName={arrServer[0]};");
        }
        else {
            builder.Append($"ServerNode={server};");
        }
        builder.Append($"UID={user};PWD={password};CS={db};");
        string connectionString = builder.ToString();
        foreach (IConnectionInfo connection in doc.DataSourceConnections) {
            var props = connection.LogonProperties;
            props.Set("Provider", "B1CRHPROXY" + (!Environment.Is64BitProcess ? "32" : ""));
            props.Set("Server Type", "B1CRHPROXY" + (!Environment.Is64BitProcess ? "32" : ""));
            props.Set("Connection String", connectionString);
            props.Set("Locale Identifier", "1033");
            connection.SetLogonProperties(props);
            connection.SetConnection(connectionString, db, user, password);
        }
    }

    public override bool HasParameters => doc.ParameterFields.Count > 0;

    public override CrystalLayoutParameters GetParameters() {
        var retVal = new CrystalLayoutParameters();
        for (int i = 0; i < doc.DataDefinition.ParameterFields.Count; i++) {
            var crystalParameter = doc.DataDefinition.ParameterFields[i];
            if (!string.IsNullOrWhiteSpace(crystalParameter.ReportName))
                continue;
            var layoutParameter = retVal.Add(i, crystalParameter.Name, crystalParameter.ValueType.ToString());
            foreach (ParameterDiscreteValue value in crystalParameter.DefaultValues)
                layoutParameter.Values.Add(value.Value.ToString(), value.Description);
        }
        return retVal;
    }

    public override void SetParameter(string id, object value) {
        var parameter     = doc.DataDefinition.ParameterFields[id];
        var currentValues = parameter.CurrentValues;
        currentValues.Clear();
        if (value != null) {
            currentValues.Add(new ParameterDiscreteValue {
                Value = value
            });
            currentValues.IsNoValue = false;
        }
        else
            currentValues.IsNoValue = true;

        parameter.ApplyCurrentValues(currentValues);
    }

    public override void SetPrinter(string name) => printReportOptions.PrinterName = name;

    public override void Print(string printerName, int copies) {
        doc.PrintOptions.PrinterName = printerName;
        doc.PrintToPrinter(copies, true, 0, 0);
    }

    public override bool ExportPDF(string filePath) {
        var exportOptions = doc.ExportOptions;
        exportOptions.ExportDestinationType = ExportDestinationType.DiskFile;
        exportOptions.ExportFormatType      = ExportFormatType.PortableDocFormat;
        exportOptions.DestinationOptions = new DiskFileDestinationOptions {
            DiskFileName = filePath
        };
        exportOptions.FormatOptions = new PdfRtfWordFormatOptions();
        doc.Export();
        return true;
    }

    public override void SetViewer(Form form) {
        form.SuspendLayout();
        var viewer = new CrystalReportViewer {
            BorderStyle = BorderStyle.FixedSingle,
            Cursor      = Cursors.Default,
            Name        = "viewer",
            Anchor      = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
        };
        //Load panels
        var pnlLeft   = form.Controls["pnlLeft"];
        var pnlRight  = form.Controls["pnlRight"];
        var pnlTop    = form.Controls["pnlTop"];
        var pnlBottom = form.Controls["pnlBottom"];
        //set viewer size
        viewer.Left   = pnlLeft.Width;
        viewer.Top    = pnlTop.Height;
        viewer.Width  = form.Width - pnlLeft.Width - pnlRight.Width;
        viewer.Height = form.Height - pnlTop.Height - pnlBottom.Height;
        //Add Control
        form.Controls.Add(viewer);
        form.Controls.SetChildIndex(viewer, 0);
        viewer.ReportSource = doc;
        foreach (Control c in viewer.Controls)
            if (c is ToolStrip strip) {
                var tsItem = strip.Items[1];
                tsItem.Click += PrintClick;
            }
        form.ResumeLayout(false);
    }

    public override void Dispose() {
        doc.Dispose();
        GC.SuppressFinalize(this);
    }
}