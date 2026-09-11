namespace Admission.Api.DTOs.Courses;

public class CourseResponse
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public decimal Fee { get; set; }

    public DateTime CreatedAt { get; set; }
}