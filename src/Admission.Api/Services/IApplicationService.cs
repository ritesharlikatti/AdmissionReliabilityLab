using Admission.Api.DTOs.Applications;
using Admission.Api.DTOs.Webhooks;
using Admission.Api.Models;
namespace Admission.Api.Services;


public interface IApplicationService
{
    Task<List<ApplicationResponse>> GetAllAsync(
        string? status,
        CancellationToken cancellationToken);
    Task<ApplicationResponse> CreateAsync(
        CreateApplicationRequest request,
        CancellationToken cancellationToken);

    Task<ApplicationResponse> RetryExternalSubmissionAsync(
        int applicationId,
        CancellationToken cancellationToken);

    Task<WebhookProcessingResult> ProcessAdmissionWebhookAsync(
        AdmissionWebhookRequest request,
        CancellationToken cancellationToken);

    Task<ReconciliationResponse> ReconcileAsync(
        int applicationId,
        CancellationToken cancellationToken);
    Task<ApplicationResponse?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken);
}