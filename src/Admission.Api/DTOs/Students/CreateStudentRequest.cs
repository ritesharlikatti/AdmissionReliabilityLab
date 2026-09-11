using System.ComponentModel.DataAnnotations;

namespace Admission.Api.DTOs.Students;

public class CreateStudentRequest
{
    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string StudentNumber { get; set; } = string.Empty;
}