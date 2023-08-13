using System;
using System.Collections.Generic;
using System.IdentityModel;
using System.IO;
using SAPbouiCOM;
using System.Linq;
using Service.Crystal.Shared;
using Service.Shared.Company;
using Service.Shared.Data;
using Service.Shared.Utils;

namespace Service.Shared.PrintLayout; 

public class Layout : IDisposable {
    public int                    Copies         { get; set; } = 1;
    public string                 Title          { get; set; }
    public int                    Type           { get; set; }
    public string                 PrinterName    { get; set; }
    public object                 Entry          { get; set; }
    public Form                   ParentForm     { get; set; }
    public string                 Filter         { get; set; }
    public SpecificFilter         SpecificFilter { get; set; }
    public event EventHandler     PrintEvent;
    public Func<int, int, string> GetDefaultPrinterFunction { get; set; }
    public int?                   DefaultLayout             { get; set; }
    public TraceObject            Tracer                    { get; set; }

    private readonly LayoutFile layoutFile = ConnectionController.DatabaseType switch {
        DatabaseType.HANA => new LayoutFileHANA(),
        DatabaseType.SQL  => new LayoutFileSQL(),
        _                 => default
    };

    private readonly Dictionary<string, CrystalLayout> layouts = new();

    private readonly List<(int id, int fileID, string fileName, string variable, string md5)> checkedFiles = new();

    private CrystalLayout currentLayout;
    private LayoutData    layoutData;
    private int?          specificID;


    public Layout(string title, int type, SpecificFilter specificFilter, Func<int, int, string> defaultPrinterFunction) {
        Title                     = title;
        Type                      = type;
        GetDefaultPrinterFunction = defaultPrinterFunction;
        SpecificFilter            = specificFilter;
    }

    public Layout(string title, int type, string printerName) {
        Title       = title;
        Type        = type;
        PrinterName = printerName;
    }

    public Layout(string title, int type, string printerName, SpecificFilter specificFilter) {
        Title          = title;
        Type           = type;
        PrinterName    = printerName;
        SpecificFilter = specificFilter;
    }

    internal static CrystalLayout CreateObject() {
        try {
            return !CompanySettings.CrystalLegacy ? new Service.Crystal.CrystalLayout() : new Service.Crystal.Legacy.CrystalLayout();
        }
        catch (Exception ex) {
            throw new Exception($"Could not create crystal report object: {ex.Message}");
        }
    }

    public void Print(bool check = true) {
        try {
            if (check)
                if (!CheckLayoutFile(OperationType.Print))
                    return;
            if (GetDefaultPrinterFunction != null) {
                Tracer?.Write("Get Default Printer", 2);
                PrinterName = GetDefaultPrinterFunction(Type, layoutData.ID);
                if (string.IsNullOrWhiteSpace(PrinterName)) {
                    Tracer?.Write("Default printer not detected, throwing exception", 2);
                    throw new PrinterNameException();
                }
            }

            LoadDocument();
            Tracer?.Write($"Printing current layout to printer: {PrinterName}", 1);
            currentLayout.Print(PrinterName, Copies);
            Tracer?.Write("Print Event Invoked", 2);
            PrintEvent?.Invoke(this, null);
            Tracer?.Write("Garbage Collector Invoked", 2);
            GC.Collect();
        }
        catch (LayoutSelectionException) {
            throw;
        }
        catch (PrinterNameException) {
            throw;
        }
        catch (Exception ex) {
            string message = $"Could not execute print operation: {ex.Message}";
            Tracer?.Write(message, 1);
            throw new Exception(message);
        }
    }


    private void LoadDocument() {
        Tracer?.Write($"Check if Layout Document for file {layoutData.FileName} is loaded", 2);
        if (layouts.ContainsKey(layoutData.MD5)) {
            currentLayout = layouts[layoutData.MD5];
            Tracer?.Write("Layout was already loaded", 2);
        }
        else {
            Tracer?.Write("Layout was not loaded, creating layout object", 2);
            currentLayout = CreateObject();
            Tracer?.Write("Loading layout file", 2);
            currentLayout.Load(layoutData.FullPath);
            Tracer?.Write("Setting layout connection", 2);
            currentLayout.SetConnection((Crystal.Shared.DatabaseType)ConnectionController.DatabaseType,
                ConnectionController.Server,
                ConnectionController.Database,
                ConnectionController.DbServerUser,
                ConnectionController.DbServerPassword);
            Tracer?.Write("Adding layout to in memory loaded layouts", 2);
            layouts.Add(layoutData.MD5, currentLayout);
        }

        Tracer?.Write($"Settings layout main parameter \"{layoutData.Variable}\": {Entry}", 2);
        currentLayout.SetParameter(layoutData.Variable, Entry);
        ApplyAdditionalParameters(currentLayout);
        Tracer?.Write("Settings layout printer", 2);
        currentLayout.SetPrinter(PrinterName);
    }

    private void ApplyAdditionalParameters(CrystalLayout layout) {
        var variables = layoutFile.GetVariables(layoutData.ID);
        if (variables.Count == 0)
            return;
        Tracer?.Write("Settings layout additional parameters", 2);
        variables.ForEach(v => {
            Tracer?.Write($"Additional Parameter \"{v.Variable}\": {v.Value}", 3);
            layout.SetParameter(v.Variable, v.Value);
        });
    }

    public bool HasActiveLayout() {
        string query = string.Format(Queries.HasActiveLayout, Type, Common.LayoutManagerUDT, Common.LayoutsTable);
        return layoutFile.GetValue<bool>(query);
    }

    private bool CheckLayoutFile(OperationType type) {
        Tracer?.Write("Check Layout File Started", 2);
        var layoutsData = LoadLayoutsData();
        Tracer?.Write("Check Layout File Procedure Executed", 2);
        switch (layoutsData.Count) {
            case 0:
                return false;
            case 1:
                Tracer?.Write("1 layout found", 2);
                LoadFile(layoutsData[0]);
                return true;
        }

        if (DefaultLayout.HasValue) {
            Tracer?.Write("Multiple layouts found, checking for default layout", 2);
            var data = layoutsData.FirstOrDefault(v => v.ID == DefaultLayout.Value);
            if (data != null) {
                LoadFile(data);
                return true;
            }
        }

        Tracer?.Write("Display print layout selection form", 2);
        throw new LayoutSelectionException(layoutsData);
    }

    private List<LayoutData> LoadLayoutsData() {
        var layoutsData = layoutFile.GetLayoutsData(Type, Filter, specificID, SpecificFilter);
        layoutsData.RemoveAll(v => string.IsNullOrWhiteSpace(v.FileName));
        var queryCheck = layoutsData.Where(v => !string.IsNullOrWhiteSpace(v.Query)).ToDictionary(layout => layout, layout => ValidateLayout(layout.Query));
        if (queryCheck.Count > 0) {
            bool hasTrueValue = queryCheck.Any(v => v.Value);
            if (!hasTrueValue)
                layoutsData.RemoveAll(v => !string.IsNullOrWhiteSpace(v.Query));
            else
                layoutsData.RemoveAll(v => string.IsNullOrWhiteSpace(v.Query) || !queryCheck[v]);
        }

        if (specificID.HasValue && layoutsData.Count == 0)
            throw new BadRequestException($"Load Specific Layout with ID {specificID} failed. Layout was not found, is not active or is a different type from requested type.");

        bool ValidateLayout(string query) {
            try {
                query = $"select ({query}) {QueryHelper.FromDummy}";
                query = query.Replace("{Key}", Entry != null ? Entry.ToString() : "");
                return layoutFile.GetValue<string>(query).ToLower().Equals("true");
            }
            catch {
                return false;
            }
        }

        return layoutsData;
    }

    private void LoadFile(LayoutData data) {
        Tracer?.Write($"Load File id: {data.ID}, fileID: {data.FileID}, fileName: {data.FileName}, variable: {data.Variable}, md5: {data.MD5}", 2);
        layoutData = data;
        if (checkedFiles.Contains((data.ID, data.FileID, data.FileName, data.Variable, data.MD5))) {
            Tracer?.Write("File already checked", 2);
            return;
        }

        if (!File.Exists(layoutData.FullPath)) {
            Tracer?.Write("File does not exists, downloading file", 2);
            DownloadFile();
        }
        else {
            Tracer?.Write("Checking file MD5 CheckSum", 2);
            byte[] content = File.ReadAllBytes(layoutData.FullPath);
            if (layoutData.MD5 != StringUtils.CreateMD5(content))
                DownloadFile();
        }

        checkedFiles.Add((data.ID, data.FileID, data.FileName, data.Variable, data.MD5));
    }

    private void DownloadFile() {
        Tracer?.Write($"Writing file: {layoutData.FullPath}", 2);
        File.WriteAllBytes(layoutData.FullPath, layoutFile.Get(layoutData.FileID));
    }

    public string ExportPDF(LayoutData data, string pdfFileName) {
        string filePath = System.IO.Path.Combine(ConnectionController.Company.AttachMentPath, $"{pdfFileName}.pdf");
        try {
            LoadFile(data);
            LoadDocument();
            if (File.Exists(filePath))
                File.Delete(filePath);
            currentLayout.ExportPDF(filePath);
        }
        catch (Exception e) {
            //todo
            throw new Exception("Export PDF not implemented, should be exported to folder for download.");
            return string.Empty;
        }

        return filePath;
    }

    public void SetLayoutID(int? id) => specificID = id;

    public void Dispose() {
        foreach (var layout in layouts.Values)
            layout.Dispose();
        layouts.Clear();
        layoutFile.Dispose();
        GC.SuppressFinalize(this);
    }
}

public class LayoutSelectionException : Exception {
    public Dictionary<int, string> Layouts { get; set; } = new();

    public LayoutSelectionException(List<LayoutData> data) => data.ForEach(value => Layouts.Add(value.ID, value.Name));
}

public class PrinterNameException : Exception {
}

public enum OperationType {
    Print,
    PrintPreview
}