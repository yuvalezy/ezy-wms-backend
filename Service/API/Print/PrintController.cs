using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing.Printing;
using System.IdentityModel;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using Newtonsoft.Json;
using Service.Shared;
using Service.Shared.Data;
using Service.Shared.PrintLayout;
using Procedure = Service.Shared.Data.Procedure;

namespace Service.API.Print; 

[Authorize]
public class PrintController : ApiController {
    private readonly DataConnector data;

    internal static readonly Dictionary<(string title, int type, string printer, string item, string bp, string bp2, string sc, string sc2), Layout> Layouts = new();

    public PrintController() => data = Global.DataObject;

    [HttpGet]
    [ActionName("Printers")]
    public IEnumerable<string> Printers([FromUri(Name = "all")] bool all = false) {
        if (LoadBalancingRouter.IsBalanced)
            return JsonConvert.DeserializeObject<string[]>(LoadBalancingRouter.SendRequest(Request));

        using var tracer = new ServiceTracer(MethodType.Get, "printers");
        if (Global.Port.HasValue)
            tracer.Write($"Node Port: {Global.Port}");
        tracer.Write("Loading Installed Printers");
        var values = PrinterSettings.InstalledPrinters.Cast<string>();
        if (!all) {
            tracer.Write("Filtering enabled printers");
            values = values.Where(v => Global.RestAPISettings.Printers.Contains(v));
        }

        tracer.Write("Returning printers list");

        return values;
    }


    [HttpGet]
    [ActionName("Objects")]
    public PrintObject[] Objects() {
        if (LoadBalancingRouter.IsBalanced)
            return JsonConvert.DeserializeObject<PrintObject[]>(LoadBalancingRouter.SendRequest(Request));

        using var tracer = new ServiceTracer(MethodType.Get, "objects");
        if (Global.Port.HasValue)
            tracer.Write($"Node Port: {Global.Port}");
        tracer.Write("Loading Objects List");
        var values = PrintObjects.ObjectsList;
        tracer.Write("Returning objects list");
        return values;
    }

    private static Layout GetLayout(PrintParameters print, SpecificFilter specific, ServiceTracer tracer) {
        tracer.Write("Get Layout - Checking if layout with specific filters combinations has been loaded previously");
        string title     = print.Type.ToString();
        int    printType = (int)print.Type;
        var    tuple     = (title, printType, print.Printer, specific?.ItemCode, specific.CardCode, specific.CardCode2, specific.ShipToCode, specific.ShipToCode2);
        if (Layouts.ContainsKey(tuple)) {
            tracer.Write("Get Layout - Specific filter configuration found, returning layout object");
            return Layouts[tuple];
        }

        tracer.Write("Get Layout - Specific filter configuration not found, creating filter combinations object");
        var layout = string.IsNullOrWhiteSpace(print.Printer)
            ? new Layout(title, printType, specific, Global.RestAPISettings.GetDefaultPrinter)
            : new Layout(title, printType, print.Printer, specific);
        tracer.Write("Get Layout - Adding filter combination to layouts control list");
        Layouts.Add(tuple, layout);
        tracer.Write("Get Layout - Returning layout object");
        return layout;
    }

    public PrintResponse Post([FromBody] PrintParameters print) {
        if (LoadBalancingRouter.IsBalanced)
            return JsonConvert.DeserializeObject<PrintResponse>(LoadBalancingRouter.SendRequest(Request, JsonConvert.SerializeObject(print)));

        var tracer = new ServiceTracer(MethodType.Post, "print");
        if (Global.Port.HasValue)
            tracer.Write($"Node Port: {Global.Port}");
        if (print == null) {
            tracer.Write("Wrong parameters structure. Cannot proceed.");
            return PrintResponse.Error("Wrong parameters structure. Cannot proceed.");
        }

        tracer.Write("Parameters received: " + JsonConvert.SerializeObject(print));

        if (!Global.PrintThread) {
            var response = ExecutePrint();
            tracer.Dispose();
            return response;
        }

        tracer.Write("Initializing Printing Execution on a separate thread Thread");
        Task.Run(() => {
            try {
                ExecutePrint();
            }
            catch (Exception e) {
                Global.LogError("Execute Print Thread Exception: " + e.Message);
            }
            finally {
                tracer.Dispose();
            }
        });
        return PrintResponse.Ok;

        PrintResponse ExecutePrint() {
            var proc = new Procedure("B1SLMAddPrintLog",
                new Parameter("ObjType", SqlDbType.Int, (int)print.Type),
                new Parameter("Entry", SqlDbType.Int, print.Entry),
                new Parameter("Printer", SqlDbType.NVarChar, 254),
                new Parameter("Status", SqlDbType.Char, 1, "S"),
                new Parameter("CurrentDate", SqlDbType.DateTime, DateTime.Now)
            );
            if (print.ID.HasValue)
                proc.Parameters.Add("ID", SqlDbType.Int, print.ID.Value);
            if (!string.IsNullOrWhiteSpace(print.Printer))
                proc["Printer"].Value = print.Printer;

            Layout layout = null;
            try {
                var specific = GetSpecificFilter(print, tracer);
                layout       = GetLayout(print, specific, tracer);
                layout.Entry = print.Entry;
                layout.SetLayoutID(print.ID);
                layout.Tracer = tracer.CreateObject("Crystal Layout Print");
                layout.Print();
                tracer.Write("Adding print log row in table @B1SLMPRINTLOG");
                proc["Printer"].Value = layout.PrinterName;
                proc.Execute();
                return PrintResponse.Ok;
            }
            catch (LayoutSelectionException ex) {
                tracer.Write($"Layout Selection Exception: {ex.Message}");
                SetError(ErrorType.Layout, ex.Message);
                return PrintResponse.LayoutSelectionError(ex.Layouts);
            }
            catch (PrinterNameException ex) {
                tracer.Write($"Print Name Exception: {ex.Message}");
                SetError(ErrorType.Printer, ex.Message);
                return PrintResponse.PrinterNameException();
            }
            catch (BadRequestException ex) {
                tracer.Write($"Bad Request Exception: {ex.Message}");
                SetError(ErrorType.Generic, ex.Message);
                return PrintResponse.Error(ex.Message);
            }
            catch (Exception ex) {
                tracer.Write($"Exception: {ex.Message}");
                SetError(ErrorType.Generic, ex.Message);
                return PrintResponse.Error(ex.Message);
            }

            void SetError(ErrorType type, string message) {
                if (layout != null && !string.IsNullOrWhiteSpace(layout.PrinterName))
                    proc["Printer"].Value = layout.PrinterName;
                proc.Parameters.Add("ErrorType", SqlDbType.Char, 1, ((char)type).ToString());
                proc.Parameters.Add("ErrorMessage", SqlDbType.NVarChar, 254, message);
                proc["Status"].Value = "E";
                tracer.Write("Adding print error log row in table @B1SLMPRINTLOG");
                proc.Execute();
            }
        }
    }

    private enum ErrorType {
        Layout  = 'L',
        Printer = 'P',
        Generic = 'G'
    }


    private SpecificFilter GetSpecificFilter(PrintParameters print, ServiceTracer tracer2) {
        void Trace(string message) => tracer2.Write("Get Specific Filters - " + message);
        return null;
        // Trace("Initializing");
        // var specific = new SpecificFilter();
        // if (print.Type == PrintObjectType.GENERIC_LAYOUTS)
        //     return specific;
        // string objType = null;
        // int?   entry   = null;
        //
        // switch (print.Type) {
        //     case PrintObjectType.ENGINEERING_BOM or PrintObjectType.DATASHEET or PrintObjectType.PLANT_MAINTENANCE_ASSET or PrintObjectType.PLANT_MAINTENANCE_REQUEST or PrintObjectType.QC_ORDER
        //         or PrintObjectType.SHOP_FLOOR_GOODS_RECEIPT or PrintObjectType.SHOP_FLOOR_GOODS_RETURN or PrintObjectType.SHOP_FLOOR_GOODS_ISSUE or PrintObjectType.CONTAINER_MASTER_DATA
        //         or PrintObjectType.CONTAINER_TRANSACTION_LOG: {
        //         Trace("Execute B1SLMGetPrintLayoutObject procedure");
        //         using var dt = data.GetDataTable("B1SLMGetPrintLayoutObject",
        //             new Parameters {
        //                 new Parameter("Type", SqlDbType.Int, (int)print.Type),
        //                 new Parameter("Entry", SqlDbType.Int, print.Entry)
        //             },
        //             CommandType.StoredProcedure);
        //         if (dt.Rows.Count > 0) {
        //             var dr = dt.Rows[0];
        //             specific.ItemCode = dr["ItemCode"].ToString();
        //
        //             switch (print.Type) {
        //                 case PrintObjectType.SHOP_FLOOR_GOODS_RECEIPT or PrintObjectType.SHOP_FLOOR_GOODS_RETURN or PrintObjectType.SHOP_FLOOR_GOODS_ISSUE:
        //                     specific.CardCode   = dr["CardCode"].ToString();
        //                     specific.ShipToCode = dr["ShipToCode"].ToString();
        //                     break;
        //                 case PrintObjectType.PLANT_MAINTENANCE_ASSET or PrintObjectType.PLANT_MAINTENANCE_REQUEST:
        //                     specific.CardCode  = dr["CardCode"].ToString();
        //                     specific.CardCode2 = dr["CardCode2"].ToString();
        //                     break;
        //                 case PrintObjectType.CONTAINER_MASTER_DATA:
        //                     objType = dr["BaseType"].ToString();
        //                     if (!string.IsNullOrWhiteSpace(objType))
        //                         entry = (int)dr["BaseEntry"];
        //                     break;
        //                 case PrintObjectType.CONTAINER_TRANSACTION_LOG:
        //                     objType = dr["ObjType"].ToString();
        //                     if (!string.IsNullOrWhiteSpace(objType))
        //                         entry = (int)dr["DocEntry"];
        //                     break;
        //             }
        //
        //             if (!string.IsNullOrWhiteSpace(specific.ItemCode))
        //                 Trace("ItemCode Loaded: " + specific.ItemCode);
        //             if (!string.IsNullOrWhiteSpace(specific.CardCode))
        //                 Trace("CardCode Loaded: " + specific.CardCode);
        //             if (!string.IsNullOrWhiteSpace(specific.ShipToCode))
        //                 Trace("ShipToCode Loaded: " + specific.ShipToCode);
        //             if (!string.IsNullOrWhiteSpace(specific.CardCode2))
        //                 Trace("CardCode2 Loaded: " + specific.CardCode2);
        //         }
        //         else {
        //             string message = $"No data was found for Entry: {print.Entry}, Object Type: {print.Type}!";
        //             Trace(message);
        //             throw new BadRequestException(message);
        //         }
        //     }
        //         break;
        //     case PrintObjectType.WMS_GOODS_RECEIPT_PO:
        //         objType = "20";
        //         entry   = print.Entry;
        //         break;
        // }
        //
        // if (string.IsNullOrWhiteSpace(objType)) {
        //     Trace("No object type detected");
        //     return specific;
        // }
        //
        // var baseObject = new BaseObject(objType, entry.Value);
        // Trace("Load Base Object Specific Values");
        // baseObject.LoadPrintSpecific();
        // specific.CardCode   = baseObject.CardCode;
        // specific.ShipToCode = baseObject.ShipToCode;
        // if (!string.IsNullOrWhiteSpace(specific.CardCode))
        //     Trace("CardCode Loaded: " + specific.CardCode);
        // if (!string.IsNullOrWhiteSpace(specific.ShipToCode))
        //     Trace("ShipToCode Loaded: " + specific.ShipToCode);
        //
        // return specific;
    }

    [HttpGet]
    [ActionName("ParametersExample")]
    public dynamic ParametersExample() => new {
        PrintParameters = new PrintParameters(PrintObjectType.HELLO_WORLD, 1, 1, "Canon Printer")
    };

    protected override void Dispose(bool disposing) {
        base.Dispose(disposing);
        data?.Dispose();
        GC.SuppressFinalize(this);
    }
}