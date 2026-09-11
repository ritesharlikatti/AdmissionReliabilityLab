using System.ComponentModel.DataAnnotations;

namespace ExternalAdmission.Api.DTOs;

public class CreateExternalApplicationRequest
{
    [Required]
    public string SourceApplicationNumber { get; set; } = string.Empty;

    [Required]
    public string StudentNumber { get; set; } = string.Empty;

    [Required]
    public string CourseCode { get; set; } = string.Empty;
}