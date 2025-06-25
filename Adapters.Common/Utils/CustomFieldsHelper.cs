using System.Data;
using System.Text;
using Core.DTOs.Items;
using Core.Interfaces;
using Core.Models.Settings;

namespace Adapters.Common.Utils;

public static class CustomFieldsHelper {
    /// <summary>
    /// Gets custom fields for a specific category from settings
    /// </summary>
    /// <param name="settings">Application settings</param>
    /// <param name="category">Category name (e.g., "Items")</param>
    /// <returns>List of custom fields for the category</returns>
    public static List<CustomField> GetCustomFields(ISettings settings, string category) {
        return settings.CustomFields?.TryGetValue(category, out var customFields) == true ? customFields.ToList() : [];
    }

    /// <summary>
    /// Appends custom field queries to a SQL query builder
    /// </summary>
    /// <param name="queryBuilder">StringBuilder containing the SQL query</param>
    /// <param name="customFields">List of custom fields to append</param>
    public static void AppendCustomFieldsToQuery(StringBuilder queryBuilder, List<CustomField> customFields) {
        foreach (var field in customFields) {
            queryBuilder.Append($", ({field.Query}) as \"{field.Key}\"");
        }
    }

    /// <summary>
    /// Reads custom field values from a data reader and populates the response object
    /// </summary>
    /// <param name="reader">Data reader containing the query results</param>
    /// <param name="customFields">List of custom fields to read</param>
    /// <param name="response">Response object that inherits from ItemResponse</param>
    /// <param name="startIndex">Starting index in the data reader for custom fields</param>
    public static void ReadCustomFields(IDataReader reader, List<CustomField> customFields, ItemResponse response, int startIndex) {
        int fieldIndex = startIndex;
        foreach (var field in customFields) {
            if (!reader.IsDBNull(fieldIndex)) {
                object value = field.Type switch {
                    CustomFieldType.Text   => reader.GetString(fieldIndex),
                    CustomFieldType.Number => reader.GetValue(fieldIndex),
                    CustomFieldType.Date   => reader.GetDateTime(fieldIndex),
                    _                      => reader.GetValue(fieldIndex)
                };
                response.CustomFields[field.Key] = value;
            }

            fieldIndex++;
        }
    }
}