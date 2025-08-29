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

        if (!IsTestEnvironment()) {
            string processFileName = Process.GetCurrentProcess().MainModule.FileName;
            path = Path.GetDirectoryName(processFileName);
        }
        else {
            // Find the solution root directory and navigate to Adapters.Windows/lib
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var solutionRoot = FindSolutionRoot(baseDir);
            path = Path.Combine(solutionRoot, "Adapters.Windows");
        }

        name = $"Interop.SAPbo{name}COM_{(!Legacy ? "100" : "93")}.dll";
        return Path.Combine(path, "lib", name);
    }

    private static bool IsTestEnvironment() {
        return Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Testing" ||
               AppDomain.CurrentDomain.BaseDirectory.Contains("UnitTests") ||
               AppDomain.CurrentDomain.BaseDirectory.Contains("Tests");
    }

    private static string FindSolutionRoot(string startPath) {
        var current = new DirectoryInfo(startPath);
        while (current != null) {
            if (current.GetFiles("*.sln").Length > 0) {
                return current.FullName;
            }
            current = current.Parent;
        }
        // Fallback: assume we're in a subdirectory of the solution
        return startPath;
    }
}