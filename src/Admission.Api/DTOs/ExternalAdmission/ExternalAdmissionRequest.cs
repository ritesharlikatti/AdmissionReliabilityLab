namespace Admission.Api.DTOs.ExternalAdmission;

public class ExternalAdmissionRequest
{
    public string SourceApplicationNumber { get; set; } = string.Empty;

    public string StudentNumber { get; set; } = string.Empty;

    public string CourseCode { get; set; } = string.Empty;
}