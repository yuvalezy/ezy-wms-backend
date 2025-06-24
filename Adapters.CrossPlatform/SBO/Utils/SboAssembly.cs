namespace Adapters.CrossPlatform.SBO.Utils;

public static class SboAssembly {
    /// <summary>
    /// Gets an indicator if the addon is running in a legacy system.
    /// For example, current SBO version is 10.0 then legacy version would be 9.x
    /// </summary>
    /// <value></value>
    /// <remarks></remarks>
    public static bool Legacy = false;

    /// <summary>
    /// Connects the application to the assembly resolver to manually load the right SAP DI API / UI API DLL
    /// </summary>
    public static void RedirectAssembly() {
        throw new NotImplementedException("Assembly redirection not implemented for cross-platform adapter");
    }

    private static string GetAssemblyPath(string name) {
        throw new NotImplementedException("Assembly path resolution not implemented for cross-platform adapter");
    }
}