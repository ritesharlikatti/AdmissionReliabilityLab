using System.ComponentModel.DataAnnotations;

namespace Admission.Api.DTOs.Applications;

public class CreateApplicationRequest
{
    [Range(1, int.MaxValue)]
    public int StudentId { get; set; }

    [Range(1, int.MaxValue)]
    public int CourseId { get; set; }

    public bool SimulateExternalTimeout { get; set; }
}