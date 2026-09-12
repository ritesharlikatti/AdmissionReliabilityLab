namespace Admission.Api.Models;

public class AdmissionApplication
{
    public int Id { get; set; }

    public string ApplicationNumber { get; set; } = string.Empty;

    public int StudentId { get; set; }

    public Student Student { get; set; } = null!;

    public int CourseId { get; set; }

    public Course Course { get; set; } = null!;

    public ApplicationStatus Status { get; set; }

    public string? ExternalApplicationId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string? IdempotencyKey { get; set; }
}