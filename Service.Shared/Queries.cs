using System;
using System.IO;
using System.Reflection;
using Service.Shared.Company;

namespace Service.Shared;

public class Queries {
    private static Assembly assembly;
    public static  string   LoadCompanyDetails   => GetEmbeddedResource("Service.Shared.Queries.LoadCompanyDetails.sql");
    public static  string   HasActiveLayout      => GetEmbeddedResource($"Service.Shared.Queries.{ConnectionController.DatabaseType}.HasActiveLayout.sql");
    public static  string   PrintLayoutVariables => GetEmbeddedResource($"Service.Shared.Queries.{ConnectionController.DatabaseType}.PrintLayoutVariables.sql");

    private static string GetEmbeddedResource(string resourceName) {
        assembly ??= typeof(Queries).Assembly;
        string resourcePath = resourceName;

        using var stream = assembly.GetManifestResourceStream(resourcePath);
        if (stream == null) {
            throw new ArgumentException($"Specified resource not found: {resourceName}");
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}