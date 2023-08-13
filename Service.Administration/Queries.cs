using System;
using System.IO;
using Service.Shared.Company;

namespace Service.Administration;

public class Queries {
    public static string ActivateDB           => GetEmbeddedResource("Service.Administration.Queries.ActivateDB.sql");
    public static string IsDBUpdate           => GetEmbeddedResource("Service.Administration.Queries.IsDBUpdate.sql");
    public static string UpdateDB             => GetEmbeddedResource("Service.Administration.Queries.UpdateDB.sql");
    public static string ExistsCommon         => GetEmbeddedResource($"Service.Administration.Queries.{ConnectionController.DatabaseType}.ExistsCommon.sql");
    public static string ExistsServiceManager => GetEmbeddedResource($"Service.Administration.Queries.{ConnectionController.DatabaseType}.ExistsServiceManager.sql");
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