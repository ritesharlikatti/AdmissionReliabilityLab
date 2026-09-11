namespace Admission.Api.DTOs.Students;

public class StudentResponse
{
    public int Id { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string StudentNumber { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}