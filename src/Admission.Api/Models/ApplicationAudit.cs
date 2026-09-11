namespace Admission.Api.Models;

public class ApplicationAudit
{
    public int Id { get; set; }

    public int ApplicationId { get; set; }

    public string EventType { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}