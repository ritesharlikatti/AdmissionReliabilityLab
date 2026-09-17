namespace Admission.Api.Models;

public class AdmissionWebhookEvent
{
    public int Id { get; set; }

    public string EventId { get; set; } = string.Empty;

    public string EventType { get; set; } = string.Empty;

    public string ExternalApplicationId { get; set; } = string.Empty;

    public DateTime OccurredAt { get; set; }

    public DateTime ProcessedAt { get; set; }
}