using System;
using System.IO;
using Service.Shared.Company;

namespace Service.Administration;

public class Queries {
    public static string ExistsCommon         => GetEmbeddedResource($"Service.Administration.Queries.{ConnectionController.DatabaseType}.ExistsCommon.sql");
    public static string ExistsDatabase       => GetEmbeddedResource($"Service.Administration.Queries.{ConnectionController.DatabaseType}.ExistsDatabase.sql");
    public static string Load                 => GetEmbeddedResource($"Service.Administration.Queries.{ConnectionController.DatabaseType}.Load.sql");

    private static string GetEmbeddedResource(string resourceName) {
        var    assembly     = typeof(Queries).Assembly;
        string resourcePath = resourceName;

        using var stream = assembly.GetManifestResourceStream(resourcePath);
        if (stream == null) {
            throw new ArgumentException($"Specified resource not found: {resourceName}");
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}