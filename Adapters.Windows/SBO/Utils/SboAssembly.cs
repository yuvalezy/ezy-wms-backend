using System.Diagnostics;
using System.Reflection;
using Microsoft.Win32;

namespace Adapters.Windows.SBO.Utils;

public static class SboAssembly {
    /// <summary>
    /// Gets an indicator if the addon is running in a legacy system.
    /// For example, current SBO version is 10.0 then legacy version would be 9.x
    /// </summary>
    /// <value></value>
    /// <remarks></remarks>
    public static bool Legacy = IsLegacy;

    private static bool IsLegacy => (string)Registry.ClassesRoot.OpenSubKey("SAPbobsCOM.Company\\CurVer").GetValue("") == $"SAPbobsCOM.Company.90.0";

    /// <summary>
    /// Connects the application to the assembly resolver to manually load the right SAP DI API / UI API DLL
    /// </summary>
    public static void RedirectAssembly() {
        AppDomain.CurrentDomain.AssemblyResolve += (sender, e) => {
            return e.Name switch {
                "Interop.SAPbobsCOM, Version=10.0.0.0, Culture=neutral, PublicKeyToken=null" => Assembly.LoadFile(GetAssemblyPath("bs")),
                "Interop.SAPbouiCOM, Version=10.0.0.0, Culture=neutral, PublicKeyToken=null" => Assembly.LoadFile(GetAssemblyPath("ui")),
                _                                                                            => null
            };
        };
    }

    private static string GetAssemblyPath(string name) {
        string? path;
        // if (Environment.UserInteractive)
        //     path = Environment.CurrentDirectory;
        // else {
            string processFileName = Process.GetCurrentProcess().MainModule.FileName;
            path = Path.GetDirectoryName(processFileName);
        // }

        name = $"Interop.SAPbo{name}COM_{(!Legacy ? "100" : "93")}.dll";
        return Path.Combine(path, "lib", name);
    }
}