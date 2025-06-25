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
            queryBuilder.Append($", ({field.Query}) as \"CustomField_{field.Key}\"");
        }
    }

    /// <summary>
    /// Reads custom field values from a data reader and populates the response object
    /// </summary>
    /// <param name="reader">Data reader containing the query results</param>
    /// <param name="customFields">List of custom fields to read</param>
    /// <param name="response">Response object that inherits from ItemResponse</param>
    public static void ReadCustomFields(IDataReader reader, List<CustomField> customFields, ItemResponse response) {
        ReadCustomFields(reader, customFields, response.CustomFields);
    }

    /// <summary>
    /// Reads custom field values from a data reader and populates the custom fields dictionary
    /// </summary>
    /// <param name="reader">Data reader containing the query results</param>
    /// <param name="customFields">List of custom fields to read</param>
    /// <param name="customFieldsDict">Dictionary to populate with custom field values</param>
    public static void ReadCustomFields(IDataReader reader, List<CustomField> customFields, Dictionary<string, object> customFieldsDict) {
        foreach (var field in customFields) {
            string columnName = $"CustomField_{field.Key}";
            if (reader[columnName] == DBNull.Value)
                continue;
            object value = field.Type switch {
                CustomFieldType.Text   => reader[columnName] as string ?? string.Empty,
                CustomFieldType.Number => reader[columnName],
                CustomFieldType.Date   => Convert.ToDateTime(reader[columnName]),
                _                      => reader[columnName]
            };
            customFieldsDict[field.Key] = value;
        }
    }

    /// <summary>
    /// Appends custom fields to a GROUP BY clause
    /// </summary>
    /// <param name="queryBuilder">StringBuilder containing the SQL query</param>
    /// <param name="customFields">List of custom fields to append to GROUP BY</param>
    public static void AppendCustomFieldsToGroupBy(StringBuilder queryBuilder, List<CustomField> customFields) {
        foreach (var field in customFields) {
            queryBuilder.Append($", {field.GroupBy ?? field.Query}");
        }
    }
}