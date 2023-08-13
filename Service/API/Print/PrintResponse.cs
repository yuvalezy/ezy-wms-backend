using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Service.API.Print; 

// ReSharper disable InconsistentNaming
public enum PrintStatus {
    ERROR,
    SUCCESS,
    MULTIPLE_LAYOUTS_ERROR,
    PRINTER_SELECTION_ERROR
}
// ReSharper restore InconsistentNaming

public class PrintResponse {
    [JsonConverter(typeof(StringEnumConverter))]
    public PrintStatus Status { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string ErrorMessage { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public List<PrintResponseLayout> Layouts { get; set; }

    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public List<string> Printers { get; set; }

    public PrintResponse() {
            
    }
    public PrintResponse(PrintStatus status) => Status = status;

    public PrintResponse(PrintStatus status, string message) {
        Status       = status;
        ErrorMessage = message;
    }

    public static PrintResponse Ok                    => new(PrintStatus.SUCCESS);
    public static PrintResponse Error(string message) => new(PrintStatus.ERROR, message);

    public static PrintResponse LayoutSelectionError(Dictionary<int, string> layouts) {
        var response = new PrintResponse(PrintStatus.MULTIPLE_LAYOUTS_ERROR, @"Multiple Layouts where found for requested parameters.
You must specify the layout ""id"" parameter to print.") { Layouts = new List<PrintResponseLayout>() };
        foreach (var keyValuePair in layouts) response.Layouts.Add(new PrintResponseLayout(keyValuePair.Key, keyValuePair.Value));
        return response;
    }

    public static PrintResponse PrinterNameException() {
        var response = new PrintResponse(PrintStatus.PRINTER_SELECTION_ERROR, @"No default printer was found for type + layout.
You must specify the desired printer you want to print with.") {
            Printers = Global.RestAPISettings.Printers
        };
        return response;
    }
}