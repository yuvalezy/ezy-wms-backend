using System;
using System.IO;

namespace Service;

public class Queries {
    public static string DatabaseSettings => GetEmbeddedResource("Service.Queries.DatabaseSettings.sql");

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