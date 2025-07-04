using System;
using System.Linq;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Service.Swagger;

/// <summary>
/// Swagger schema filter to enhance enum documentation
/// </summary>
public class EnumSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type.IsEnum)
        {
            schema.Enum.Clear();
            schema.Type = "string";
            schema.Format = null;
            
            var enumValues = Enum.GetValues(context.Type);
            foreach (var enumValue in enumValues)
            {
                schema.Enum.Add(new Microsoft.OpenApi.Any.OpenApiString(enumValue.ToString()));
            }
            
            // Add description with enum values
            var enumNames = Enum.GetNames(context.Type);
            var enumDescriptions = enumNames.Select(name => $"- {name}").ToList();
            schema.Description = $"Allowed values:\n{string.Join("\n", enumDescriptions)}";
        }
    }
}