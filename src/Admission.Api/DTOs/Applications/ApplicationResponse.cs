namespace Admission.Api.DTOs.Applications;

public class ApplicationResponse
{
    public int Id { get; set; }

    public string ApplicationNumber { get; set; } = string.Empty;

    public int StudentId { get; set; }

    public int CourseId { get; set; }

    public string Status { get; set; } = string.Empty;
    
    public string? ExternalApplicationId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}