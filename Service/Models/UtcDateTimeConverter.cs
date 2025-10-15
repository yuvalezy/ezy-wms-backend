using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Service.Models;

public class UtcDateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.GetString() is { } dateString)
        {
            // Handle dates with or without Z suffix
            if (DateTime.TryParse(dateString, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var date))
            {
                // Ensure it's treated as UTC
                return date.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(date, DateTimeKind.Utc) : date.ToUniversalTime();
            }
        }
        
        return DateTime.MinValue;
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        // All DateTime values in our database are stored as UTC (using DateTime.UtcNow and GETUTCDATE())
        // When EF Core reads them back, they may have Kind=Unspecified or Kind=Local
        // We should treat them as UTC values and NOT call ToUniversalTime() which would add the UTC offset
        var utcDateTime = value.Kind == DateTimeKind.Utc
            ? value
            : DateTime.SpecifyKind(value, DateTimeKind.Utc);

        // Write in ISO 8601 format with Z suffix
        writer.WriteStringValue(utcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.FFF'Z'", CultureInfo.InvariantCulture));
    }
}

public class NullableUtcDateTimeConverter : JsonConverter<DateTime?>
{
    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;
            
        if (reader.GetString() is { } dateString)
        {
            // Handle dates with or without Z suffix
            if (DateTime.TryParse(dateString, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var date))
            {
                // Ensure it's treated as UTC
                return date.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(date, DateTimeKind.Utc) : date.ToUniversalTime();
            }
        }
        
        return null;
    }

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (!value.HasValue)
        {
            writer.WriteNullValue();
            return;
        }

        // All DateTime values in our database are stored as UTC (using DateTime.UtcNow and GETUTCDATE())
        // When EF Core reads them back, they may have Kind=Unspecified or Kind=Local
        // We should treat them as UTC values and NOT call ToUniversalTime() which would add the UTC offset
        var utcDateTime = value.Value.Kind == DateTimeKind.Utc
            ? value.Value
            : DateTime.SpecifyKind(value.Value, DateTimeKind.Utc);

        // Write in ISO 8601 format with Z suffix
        writer.WriteStringValue(utcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss.FFF'Z'", CultureInfo.InvariantCulture));
    }
}