namespace Admission.Api.Models;

public class Course
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public decimal Fee { get; set; }

    public DateTime CreatedAt { get; set; }

    public ICollection<AdmissionApplication> Applications { get; set; }
        = new List<AdmissionApplication>();
}