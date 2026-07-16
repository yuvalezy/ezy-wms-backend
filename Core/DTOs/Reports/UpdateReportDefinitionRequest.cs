using System.ComponentModel.DataAnnotations;

namespace Core.DTOs.Reports;

/// <summary>
/// Superuser payload to update a report. Columns and variables are replaced wholesale by
/// <see cref="CreateReportDefinitionRequest.Columns"/> / <see cref="CreateReportDefinitionRequest.Variables"/>.
/// Report grants live in the authorization-group join table and are untouched by this.
/// </summary>
public class UpdateReportDefinitionRequest : CreateReportDefinitionRequest {
    [Required]
    public Guid Id { get; set; }
}
