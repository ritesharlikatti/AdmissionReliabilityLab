using System.ComponentModel.DataAnnotations;

namespace Admission.Api.DTOs.Courses;

public class CreateCourseRequest
{
    [Required]
    [MaxLength(30)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Range(0, double.MaxValue)]
    public decimal Fee { get; set; }
}