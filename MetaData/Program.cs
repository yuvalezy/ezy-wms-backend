using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using MetaData.Data;
using Service.Shared.Data;

namespace MetaData;

internal class Program {
    public static void Main(string[] args) {
        SBOAssembly.RedirectAssembly();
        Thread.CurrentThread.CurrentCulture   = new CultureInfo("es-PA");
        Thread.CurrentThread.CurrentUICulture = new CultureInfo("es-PA");
        Run(args.FirstOrDefault());
    }

    private static void Run(string arg) {
        switch (arg) {
            case "import":
                var import = new Import();
                import.Run();
                break;
            case "export":
                var export = new Export();
                export.Run();
                break;
            default:
                Console.WriteLine("Valid commands: import, export");
                break;
        }
        Console.WriteLine("Press any key to exit.");
        Console.Read();
    }
}