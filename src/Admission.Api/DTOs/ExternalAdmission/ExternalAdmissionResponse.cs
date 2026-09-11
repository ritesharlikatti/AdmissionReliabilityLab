namespace Admission.Api.DTOs.ExternalAdmission;

public class ExternalAdmissionResponse
{
    public int Id { get; set; }

    public string ExternalApplicationId { get; set; } = string.Empty;

    public string SourceApplicationNumber { get; set; } = string.Empty;

    public string StudentNumber { get; set; } = string.Empty;

    public string CourseCode { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime ReceivedAt { get; set; }
}