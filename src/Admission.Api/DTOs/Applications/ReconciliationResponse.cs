namespace Admission.Api.DTOs.Applications;

public class ReconciliationResponse
{
    public int ApplicationId { get; set; }

    public string ApplicationNumber { get; set; } = string.Empty;

    public string ExternalApplicationId { get; set; } = string.Empty;

    public string LocalStatusBefore { get; set; } = string.Empty;

    public string ExternalStatus { get; set; } = string.Empty;

    public string LocalStatusAfter { get; set; } = string.Empty;

    public bool Changed { get; set; }

    public string Message { get; set; } = string.Empty;
}