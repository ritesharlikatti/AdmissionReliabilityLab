namespace Admission.Api.Models;

public class Student
{
    public int Id { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string StudentNumber { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public ICollection<AdmissionApplication> Applications { get; set; }
    = new List<AdmissionApplication>();
        
}