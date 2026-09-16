namespace Admission.Api.DTOs.Webhooks;

public class AdmissionWebhookRequest
{
    public string EventId { get; set; } = string.Empty;

    public string EventType { get; set; } = string.Empty;

    public string ExternalApplicationId { get; set; }
        = string.Empty;

    public string SourceApplicationNumber { get; set; }
        = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime OccurredAt { get; set; }
}